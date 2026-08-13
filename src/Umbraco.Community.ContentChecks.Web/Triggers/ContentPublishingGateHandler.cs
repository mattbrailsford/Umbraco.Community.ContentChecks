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

        var blockingEntries = new List<ContentCheckPublishGateEntry>();
        var recommendedMissedEntries = new List<ContentCheckPublishGateEntry>();
        foreach (var content in notification.PublishedEntities)
        {
            foreach (var culture in CulturesBeingPublished(notification, content))
            {
                var result = await _gateService.EvaluateAsync(content, culture, segment: null, currentUserKey, cancellationToken);
                blockingEntries.AddRange(result.BlockingEntries);
                recommendedMissedEntries.AddRange(result.RecommendedMissedEntries);
            }
        }

        // A block supersedes a recommendation notice -- never show both for the same publish
        // attempt. Deliberately generic, not per-check detail: the Checks panel is where an editor
        // reads the actual reasons; a toast only needs to tell them to go look.
        if (blockingEntries.Count > 0)
        {
            notification.CancelOperation(new EventMessage(
                "Content Checks",
                $"{blockingEntries.Count} check{(blockingEntries.Count == 1 ? "" : "s")} {(blockingEntries.Count == 1 ? "is" : "are")} blocking this publish — see the Checks panel for details.",
                EventMessageType.Error));
        }
        else if (recommendedMissedEntries.Count > 0)
        {
            // Not cancelled -- this is advisory. Adding to Messages without calling
            // CancelOperation rides the same ambient EventMessages/Umb-Notifications plumbing as a
            // second, non-blocking toast (the same pattern Umbraco's own
            // AddDomainWarningsWhenPublishingNotificationHandler uses for its own publish warnings).
            notification.Messages.Add(new EventMessage(
                "Content Checks",
                $"{recommendedMissedEntries.Count} recommended check{(recommendedMissedEntries.Count == 1 ? "" : "s")} {(recommendedMissedEntries.Count == 1 ? "was" : "were")} missed — see the Checks panel for details.",
                EventMessageType.Warning));
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
