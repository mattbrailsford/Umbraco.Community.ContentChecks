namespace Umbraco.Community.ContentChecks.Core.Checks;

/// <summary>
/// Runs a set of resolved rules against one content snapshot and returns a report. Deliberately
/// scoped to "one snapshot, one set of resolved rules, one report" — element-type cascading and
/// composition resolution call this once per discovered snapshot and aggregate; this runner
/// never needs to know that layer exists.
/// </summary>
public interface IContentCheckRunner
{
    /// <summary>
    /// Runs the given rules against the given content.
    /// </summary>
    /// <param name="request">The rules to run and the content to run them against.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The resulting report.</returns>
    Task<ContentCheckReport> RunAsync(ContentCheckRunRequest request, CancellationToken cancellationToken);
}
