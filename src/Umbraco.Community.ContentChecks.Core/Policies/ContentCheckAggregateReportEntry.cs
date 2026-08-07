using Umbraco.Community.ContentChecks.Core.Checks;

namespace Umbraco.Community.ContentChecks.Core.Policies;

/// <summary>
/// One <see cref="ContentCheckReportEntry"/> stamped with the location it ran against — empty
/// <see cref="Path"/> for the root item itself, one entry per level of nesting otherwise.
/// </summary>
public sealed class ContentCheckAggregateReportEntry
{
    /// <summary>
    /// The underlying report entry, unchanged from <see cref="IContentCheckRunner"/>'s own output.
    /// </summary>
    public required ContentCheckReportEntry Entry { get; init; }

    /// <summary>
    /// The root-to-here chain of nested elements the snapshot this entry ran against was
    /// discovered through. Empty means the root item itself.
    /// </summary>
    public required IReadOnlyList<ContentCheckLocationStep> Path { get; init; }
}
