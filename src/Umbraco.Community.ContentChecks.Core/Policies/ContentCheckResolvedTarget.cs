using Umbraco.Community.ContentChecks.Core.Checks;

namespace Umbraco.Community.ContentChecks.Core.Policies;

/// <summary>
/// One snapshot discovered by <see cref="IContentCheckResolutionService"/> — the root document, or
/// one nested element found by cascading — paired with the rules that apply to it. Feed each of
/// these into <see cref="IContentCheckRunner"/> exactly as it would run the root document; the
/// runner needs no changes to support this (see the Policy Resolution spec's cascading section).
/// Aggregating the resulting reports is a caller concern, deferred until the UI that needs a
/// specific shape actually exists.
/// </summary>
public sealed class ContentCheckResolvedTarget
{
    /// <summary>
    /// The snapshot to run checks against — the root document's own snapshot, or a nested element's.
    /// </summary>
    public required IContentCheckSnapshot Snapshot { get; init; }

    /// <summary>
    /// The rules that apply to this snapshot.
    /// </summary>
    public required IReadOnlyList<ResolvedContentCheckRule> Rules { get; init; }

    /// <summary>
    /// For a nested element, the alias of the property (on whichever type directly contains it)
    /// it was discovered through. <c>null</c> for the root document's own target.
    /// </summary>
    public string? ContainingPropertyAlias { get; init; }
}
