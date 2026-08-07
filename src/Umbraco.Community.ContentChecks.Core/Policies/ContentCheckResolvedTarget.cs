using Umbraco.Community.ContentChecks.Core.Checks;

namespace Umbraco.Community.ContentChecks.Core.Policies;

/// <summary>
/// One snapshot discovered by <see cref="IContentCheckResolutionService"/> — the root document, or
/// one nested element found by cascading — paired with every policy that matched it. Feed each of
/// these into <see cref="IContentCheckRunner"/> exactly as it would run the root document; the
/// runner needs no changes to support this (see the Policy Resolution spec's cascading section).
/// </summary>
public sealed class ContentCheckResolvedTarget
{
    /// <summary>
    /// The snapshot to run checks against — the root document's own snapshot, or a nested element's.
    /// </summary>
    public required IContentCheckSnapshot Snapshot { get; init; }

    /// <summary>
    /// Every policy that matched this snapshot, grouped with its own provenance and resolved
    /// rules. This is the accurate, per-item picture a preview needs — nothing here can suffer
    /// the "shows up but doesn't really apply" problem a schema-level view would have, since it
    /// only ever reflects real matches against this real item's real path.
    /// </summary>
    public required IReadOnlyList<ContentCheckResolvedPolicyMatch> PolicyMatches { get; init; }

    /// <summary>
    /// The root-to-here chain of nested elements this target was discovered through. Empty for
    /// the root item's own target; one entry per level of cascading otherwise.
    /// </summary>
    public required IReadOnlyList<ContentCheckLocationStep> Path { get; init; }
}
