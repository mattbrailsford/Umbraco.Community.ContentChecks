using Umbraco.Community.ContentChecks.Web.Api.Management.Evaluation.Models;

namespace Umbraco.Community.ContentChecks.Web.Runs;

/// <summary>
/// Pushes a run's progress to whichever backoffice clients are watching it, over
/// <see cref="ContentCheckRunHub"/>. The background worker is the only caller — it already has
/// the up-to-date state in hand from mutating <see cref="ContentCheckRunTracker"/>, so this takes
/// already-built response models rather than resolving anything itself.
/// </summary>
public interface IContentCheckRunNotifier
{
    /// <summary>
    /// Notifies watchers of a run that one of its items changed — new progress, a finished
    /// report, or a failure.
    /// </summary>
    /// <param name="runId">The run's id.</param>
    /// <param name="item">The item's current state.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task NotifyItemChangedAsync(Guid runId, EvaluationItemResponseModel item, CancellationToken cancellationToken);

    /// <summary>
    /// Notifies watchers of a run that every item has finished.
    /// </summary>
    /// <param name="run">The run's final state.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task NotifyCompleteAsync(EvaluationRunResponseModel run, CancellationToken cancellationToken);
}
