namespace Umbraco.Community.ContentChecks.Core.Checks;

/// <summary>
/// The outcome of running a set of resolved rules against one content snapshot.
/// </summary>
public sealed class ContentCheckReport
{
    /// <summary>
    /// One entry per rule that was run.
    /// </summary>
    public required IReadOnlyList<ContentCheckReportEntry> Entries { get; init; }
}
