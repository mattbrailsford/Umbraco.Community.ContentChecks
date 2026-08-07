namespace Umbraco.Community.ContentChecks.Web.Runs;

/// <summary>
/// Queues a run id for the background worker to pick up. The endpoint that starts a run has
/// already registered it with <see cref="IContentCheckRunTracker"/> before enqueuing it here —
/// this just decouples "a run exists" from "the worker has gotten to it yet".
/// </summary>
public interface IContentCheckRunQueue
{
    /// <summary>
    /// Enqueues a run for the background worker to process.
    /// </summary>
    /// <param name="runId">The run's identity.</param>
    void Enqueue(Guid runId);

    /// <summary>
    /// Waits for the next queued run.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The next run's identity.</returns>
    Task<Guid> DequeueAsync(CancellationToken cancellationToken);
}
