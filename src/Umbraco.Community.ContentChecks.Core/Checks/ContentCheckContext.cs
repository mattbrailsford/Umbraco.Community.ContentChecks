namespace Umbraco.Community.ContentChecks.Core.Checks;

/// <summary>
/// What a check evaluates: a resolved snapshot of exactly one thing (a root document or one
/// nested element), plus the variant being checked. Carrying a resolved snapshot rather than an
/// id lets a Draft Set's pending state, an unsaved editor draft, or trunk content all be
/// validated through the same path (finding L).
/// </summary>
public sealed class ContentCheckContext
{
    /// <summary>
    /// The content being evaluated.
    /// </summary>
    public required IContentCheckSnapshot Content { get; init; }

    /// <summary>
    /// The culture being evaluated, or <c>null</c> for an invariant check.
    /// </summary>
    public string? Culture { get; init; }

    /// <summary>
    /// The segment being evaluated, or <c>null</c> for an unsegmented check.
    /// </summary>
    public string? Segment { get; init; }
}
