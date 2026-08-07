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
}
