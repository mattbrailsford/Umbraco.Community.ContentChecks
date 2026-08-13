using Microsoft.AspNetCore.Http;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Security;
using Umbraco.Community.ContentChecks.Core.Policies;
using Umbraco.Extensions;

namespace Umbraco.Community.ContentChecks.Web.Triggers;

/// <summary>
/// The publish gate — cancels a publish when the content's most recently stored check report
/// blocks the current user, per docs/guard-spec.md. Never evaluates checks itself; only reads
/// what a prior run already produced (<see cref="IContentCheckPublishGateService"/>).
/// </summary>
internal sealed class ContentPublishingGateHandler : INotificationAsyncHandler<ContentPublishingNotification>
{
    private readonly IContentCheckPublishGateService _gateService;
    private readonly IBackOfficeSecurityAccessor _backOfficeSecurityAccessor;
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentPublishingGateHandler"/> class.
    /// </summary>
    /// <param name="gateService">Resolves whether a content item's stored report blocks the current user.</param>
    /// <param name="backOfficeSecurityAccessor">Resolves the user attempting to publish.</param>
    /// <param name="httpContextAccessor">Used to detect non-interactive (scheduled/background) publishes, which never block.</param>
    public ContentPublishingGateHandler(
        IContentCheckPublishGateService gateService,
        IBackOfficeSecurityAccessor backOfficeSecurityAccessor,
        IHttpContextAccessor httpContextAccessor)
    {
        _gateService = gateService;
        _backOfficeSecurityAccessor = backOfficeSecurityAccessor;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public async Task HandleAsync(ContentPublishingNotification notification, CancellationToken cancellationToken)
    {
        // No live request behind this publish (e.g. a scheduled publish, or a branch publish
        // running on a background thread) -- never blocks, matching every other non-interactive
        // publish path. See docs/triggers-spec.md, finding E: scheduled/bulk/descendants/API-driven
        // publishing gets recorded (via on-save/on-demand runs), never blocked.
        if (_httpContextAccessor.HttpContext is null)
        {
            return;
        }

        var currentUserKey = _backOfficeSecurityAccessor.BackOfficeSecurity?.CurrentUser?.Key;

        var blockingEntries = new List<ContentCheckPublishGateBlockingEntry>();
        foreach (var content in notification.PublishedEntities)
        {
            foreach (var culture in CulturesBeingPublished(notification, content))
            {
                var result = await _gateService.EvaluateAsync(content, culture, segment: null, currentUserKey, cancellationToken);
                blockingEntries.AddRange(result.BlockingEntries);
            }
        }

        if (blockingEntries.Count > 0)
        {
            var reason = string.Join(" | ", blockingEntries.Select(e => $"{e.CheckAlias}: {e.Message}"));
            notification.CancelOperation(new EventMessage("Content Checks", reason, EventMessageType.Error));
        }
    }

    /// <summary>
    /// The cultures actually being published for this item -- every available culture for variant
    /// content, filtered down to the ones this operation is publishing; a single <c>null</c> culture
    /// for invariant content.
    /// </summary>
    private static IEnumerable<string?> CulturesBeingPublished(ContentPublishingNotification notification, IContent content)
    {
        if (!content.ContentType.VariesByCulture())
        {
            return new string?[] { null };
        }

        return content.AvailableCultures
            .Where(culture => notification.IsPublishingCulture(content, culture))
            .Cast<string?>()
            .ToList();
    }
}
