namespace Umbraco.Community.ContentChecks.Core.Policies;

/// <summary>
/// How a policy came to govern a content type in an Effective Checks computation — three
/// genuinely different things happening at different snapshot levels, not shades of the same
/// "inherited" idea.
/// </summary>
public enum ContentCheckEffectiveCheckProvenance
{
    /// <summary>
    /// The policy targets this type exactly.
    /// </summary>
    Direct,

    /// <summary>
    /// The policy targets a type this one composes from, directly or transitively. Runs against
    /// this type's own snapshot — composition flattens properties onto the root, so nothing is nested.
    /// </summary>
    Composition,

    /// <summary>
    /// The policy targets an element type discoverable inside this type's own block structure.
    /// Runs against a separate, nested snapshot.
    /// </summary>
    Cascading,
}
