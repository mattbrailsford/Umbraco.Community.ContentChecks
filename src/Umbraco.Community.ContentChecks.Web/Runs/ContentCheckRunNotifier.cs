using Microsoft.AspNetCore.SignalR;
using Umbraco.Community.ContentChecks.Web.Api.Management.Evaluation.Models;

namespace Umbraco.Community.ContentChecks.Web.Runs;

/// <summary>
/// Default implementation of <see cref="IContentCheckRunNotifier"/>, backed by
/// <see cref="IHubContext{THub}"/> for <see cref="ContentCheckRunHub"/>.
/// </summary>
internal sealed class ContentCheckRunNotifier : IContentCheckRunNotifier
{
    private readonly IHubContext<ContentCheckRunHub> _hubContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentCheckRunNotifier"/> class.
    /// </summary>
    /// <param name="hubContext">The hub context to broadcast through.</param>
    public ContentCheckRunNotifier(IHubContext<ContentCheckRunHub> hubContext)
    {
        _hubContext = hubContext;
    }

    /// <inheritdoc />
    public Task NotifyItemChangedAsync(Guid runId, EvaluationItemResponseModel item, CancellationToken cancellationToken)
        => _hubContext.Clients.Group(ContentCheckRunHub.GroupName(runId)).SendAsync("progress", item, cancellationToken);

    /// <inheritdoc />
    public Task NotifyCompleteAsync(EvaluationRunResponseModel run, CancellationToken cancellationToken)
        => _hubContext.Clients.Group(ContentCheckRunHub.GroupName(run.RunId)).SendAsync("complete", run, cancellationToken);
}
