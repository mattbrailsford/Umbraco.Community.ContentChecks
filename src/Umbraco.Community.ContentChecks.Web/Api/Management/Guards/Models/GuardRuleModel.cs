using Umbraco.Community.ContentChecks.Core.Checks;

namespace Umbraco.Community.ContentChecks.Web.Api.Management.Guards.Models;

/// <summary>
/// One rule within a guard — shared shape for both request and response.
/// </summary>
public class GuardRuleModel
{
    /// <summary>
    /// The rule's own durable identity, distinct from the owning guard's <c>Key</c>. On a request,
    /// an empty/omitted value means "this is a new rule" and a fresh one is assigned; an existing
    /// rule being edited should round-trip whatever value it was given in the response it came from.
    /// </summary>
    public Guid Key { get; set; }

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
    /// The lowest <see cref="RequirementLevel"/> that blocks. <c>null</c> means "never block."
    /// </summary>
    public RequirementLevel? BlockAt { get; set; }
}
