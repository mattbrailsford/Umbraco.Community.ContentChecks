using Umbraco.Community.ContentChecks.Core.Checks;

namespace Umbraco.Community.ContentChecks.Core.Policies;

/// <summary>
/// One policy that matched a <see cref="ContentCheckResolvedTarget"/>, together with why it
/// matched and which of its rules resolved to a registered check. Resolution keeps this reasoning
/// instead of throwing it away, so a per-item preview can render *why* each policy applies before
/// anything has run — the same data <see cref="IContentCheckRunner"/> ends up running, not an
/// approximation of it.
/// </summary>
public sealed class ContentCheckResolvedPolicyMatch
{
    /// <summary>
    /// The policy that matched.
    /// </summary>
    public required ContentCheckPolicy Policy { get; init; }

    /// <summary>
    /// How this policy came to match — Direct, Composition, or Cascading. Reuses
    /// <see cref="ContentCheckEffectiveCheckProvenance"/> rather than a second, parallel enum.
    /// </summary>
    public required ContentCheckEffectiveCheckProvenance Provenance { get; init; }

    /// <summary>
    /// The policy's rules that resolved to a registered check.
    /// </summary>
    public required IReadOnlyList<ResolvedContentCheckRule> Rules { get; init; }
}
