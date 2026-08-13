using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Web.Common.Authorization;
using Umbraco.Community.ContentChecks.Web.Api.Management.Evaluation.Models;
using Umbraco.Community.ContentChecks.Web.Authorization;

namespace Umbraco.Community.ContentChecks.Web.Runs;

/// <summary>
/// Pushes a run's live progress to whichever backoffice clients are watching it — replaces
/// polling a Server-Sent Events endpoint. A client subscribes via <see cref="JoinRun"/>;
/// <see cref="IContentCheckRunNotifier"/> is what pushes further updates into the run's group
/// afterward, from the background worker that's actually running the checks.
/// </summary>
[Authorize(Policy = AuthorizationPolicies.BackOfficeAccess)]
[Authorize(Policy = ContentChecksAuthorizationPolicies.SectionAccessContentChecks)]
public sealed class ContentCheckRunHub : Hub
{
    private readonly IContentCheckRunTracker _tracker;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentCheckRunHub"/> class.
    /// </summary>
    /// <param name="tracker">The live-run registry.</param>
    /// <param name="umbracoMapper">Maps run state to the same API response models the REST endpoints use.</param>
    public ContentCheckRunHub(IContentCheckRunTracker tracker, IUmbracoMapper umbracoMapper)
    {
        _tracker = tracker;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Subscribes the caller to a run's updates, and immediately replays whatever's already known
    /// about it. A client can connect after some (or all) of a run's items have already finished —
    /// a fast run, or a slow page load — so it must see their state too, not just what happens from
    /// the moment it joins.
    /// </summary>
    /// <param name="runId">The run's id.</param>
    public async Task JoinRun(Guid runId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(runId));

        var run = _tracker.GetRun(runId);
        if (run is null)
        {
            return;
        }

        foreach (var item in run.Items.Values)
        {
            await Clients.Caller.SendAsync("progress", _umbracoMapper.Map<EvaluationItemResponseModel>(item));
        }

        if (run.Status == ContentCheckRunStatus.Completed)
        {
            await Clients.Caller.SendAsync("complete", _umbracoMapper.Map<EvaluationRunResponseModel>(run));
        }
    }

    /// <summary>
    /// The SignalR group a run's updates are broadcast to — one group per run, so watchers of
    /// different runs never see each other's events.
    /// </summary>
    /// <param name="runId">The run's id.</param>
    /// <returns>The group name.</returns>
    internal static string GroupName(Guid runId) => $"run-{runId}";
}
