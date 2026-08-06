namespace Umbraco.Community.ContentChecks.Core.Checks;

/// <summary>
/// A request to run a set of resolved rules against one content snapshot.
/// </summary>
public sealed class ContentCheckRunRequest
{
    /// <summary>
    /// The content being evaluated.
    /// </summary>
    public required IContentCheckSnapshot Content { get; init; }

    /// <summary>
    /// The culture being evaluated, or <c>null</c> for an invariant run.
    /// </summary>
    public string? Culture { get; init; }

    /// <summary>
    /// The segment being evaluated, or <c>null</c> for an unsegmented run.
    /// </summary>
    public string? Segment { get; init; }

    /// <summary>
    /// The rules to run.
    /// </summary>
    public required IReadOnlyList<ResolvedContentCheckRule> Rules { get; init; }
}
