using Umbraco.Community.ContentChecks.Core.Checks;

namespace Umbraco.Community.ContentChecks.Core.Policies;

/// <summary>
/// Decides, for one section of the site, what actually happens when a rule at a given
/// <see cref="RequirementLevel"/> fails. Deliberately separate from <see cref="ContentCheckPolicy"/>
/// — a policy says what to check; a guard says how hard it's enforced, and for whom. See
/// docs/guard-spec.md.
/// </summary>
public sealed class Guard
{
    /// <summary>
    /// The guard's durable identity.
    /// </summary>
    public Guid Key { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The display name of the guard.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Restricts the guard to content underneath this node. <c>null</c> means the whole site.
    /// </summary>
    public Guid? StartNodeKey { get; set; }

    /// <summary>
    /// The rules this guard resolves against, most specific first. See
    /// <see cref="GuardRule"/> for how a rule is matched to the current user.
    /// </summary>
    public IReadOnlyList<GuardRule> Rules { get; set; } = [];
}
