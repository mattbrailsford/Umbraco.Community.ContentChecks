namespace Umbraco.Community.ContentChecks.Web.Runs;

/// <summary>
/// The in-memory registry of live-run state — a singleton, Web-only, not persisted. The primitive
/// both the Management API endpoints and the background worker read and write against, so a
/// stateless HTTP endpoint can hand back a run id immediately and let the caller poll or stream
/// progress afterward.
/// </summary>
public interface IContentCheckRunTracker
{
    /// <summary>
    /// Registers a new run for the given content items and returns its initial state.
    /// </summary>
    /// <param name="contentKeys">The content items to evaluate.</param>
    /// <returns>The new run's state, with every item pending.</returns>
    ContentCheckRunState StartRun(IReadOnlyList<Guid> contentKeys);

    /// <summary>
    /// Gets a run's current state.
    /// </summary>
    /// <param name="runId">The run's identity.</param>
    /// <returns>The run's state, or <c>null</c> if no run with this id is known.</returns>
    ContentCheckRunState? GetRun(Guid runId);
}
