using Umbraco.Community.ContentChecks.Core.Checks;

namespace Umbraco.Community.ContentChecks.Core.Policies;

/// <summary>
/// One rule within an <see cref="Guard"/> — says the lowest <see cref="RequirementLevel"/>
/// that blocks, for a specific user, a user group ("role"), or for everyone else. A rule targets at
/// most one of <see cref="UserKey"/>/<see cref="RoleKey"/>; when both are <c>null</c> it's the
/// catch-all rule. Most specific match wins — user rule beats role rule beats catch-all — see
/// docs/guard-spec.md.
/// </summary>
public sealed class GuardRule
{
    /// <summary>
    /// The user this rule applies to, or <c>null</c> if this rule isn't user-scoped.
    /// </summary>
    public Guid? UserKey { get; set; }

    /// <summary>
    /// The user group (role) this rule applies to, or <c>null</c> if this rule isn't role-scoped.
    /// Ignored when <see cref="UserKey"/> is set.
    /// </summary>
    public Guid? RoleKey { get; set; }

    /// <summary>
    /// The lowest <see cref="RequirementLevel"/> that blocks — a failing check at this level or
    /// higher blocks; anything below it only warns. <c>null</c> means "never block" (everything
    /// warns).
    /// </summary>
    public RequirementLevel? BlockAt { get; set; }
}
