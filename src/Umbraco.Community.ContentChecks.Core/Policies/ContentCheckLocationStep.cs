namespace Umbraco.Community.ContentChecks.Core.Policies;

/// <summary>
/// One step in the root-to-here chain of nested elements a <see cref="ContentCheckResolvedTarget"/>
/// was discovered through — which property held it, and which real element instance was found
/// there. An empty path means the root item's own target; each level of cascading adds one step.
/// </summary>
public sealed class ContentCheckLocationStep
{
    /// <summary>
    /// The alias of the property (on whichever type directly contains it) the element was
    /// discovered through.
    /// </summary>
    public required string PropertyAlias { get; init; }

    /// <summary>
    /// The Key of the specific element instance found at this step.
    /// </summary>
    public required Guid ElementKey { get; init; }

    /// <summary>
    /// This instance's 1-based position among every element found at <see cref="PropertyAlias"/>,
    /// in document order — e.g. the second Hero block on the page. For a Block Grid this reflects
    /// reading order, not row/column position; good enough to tell instances of the same type
    /// apart without teaching resolution about any one element source's own layout structure.
    /// A path persisted before this field existed deserializes to 1 -- indistinguishable from a
    /// genuine first instance, which is an acceptable stale read for old data.
    /// </summary>
    public int Index { get; init; } = 1;
}
