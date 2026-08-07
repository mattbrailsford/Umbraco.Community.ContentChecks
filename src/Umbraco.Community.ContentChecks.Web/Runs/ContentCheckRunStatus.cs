namespace Umbraco.Community.ContentChecks.Web.Runs;

/// <summary>
/// Whether a live-run is still going, or every item in it has finished (successfully or not).
/// </summary>
public enum ContentCheckRunStatus
{
    /// <summary>
    /// At least one item in the run hasn't finished yet.
    /// </summary>
    Running,

    /// <summary>
    /// Every item in the run has either produced a report or failed.
    /// </summary>
    Completed,
}
