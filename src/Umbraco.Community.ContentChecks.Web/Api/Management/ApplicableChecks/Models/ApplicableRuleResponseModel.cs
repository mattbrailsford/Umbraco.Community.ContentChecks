using System.ComponentModel.DataAnnotations;
using Umbraco.Community.ContentChecks.Core.Checks;

namespace Umbraco.Community.ContentChecks.Web.Api.Management.ApplicableChecks.Models;

/// <summary>
/// One resolved rule within an <see cref="ApplicablePolicyMatchResponseModel"/> — enough for a
/// pending row before anything has run: which check, and how strictly it's enforced. The rule's
/// own config isn't included here — this endpoint answers "what applies and why", not "how is it
/// configured," which is the policy editor's job, not the per-item panel's.
/// </summary>
public class ApplicableRuleResponseModel
{
    /// <summary>
    /// The rule's own durable identity — not the config itself, just enough for the client to
    /// tell two rules apart that both run the same check at the same location with different
    /// config (e.g. two "required property" rules checking different properties), and to match a
    /// result back to the exact rule that produced it once a run completes.
    /// </summary>
    public Guid RuleKey { get; set; }

    /// <summary>
    /// The alias of the check this rule runs.
    /// </summary>
    [Required]
    public string CheckAlias { get; set; } = string.Empty;

    /// <summary>
    /// The display name of the check this rule runs.
    /// </summary>
    [Required]
    public string CheckName { get; set; } = string.Empty;

    /// <summary>
    /// How important a failure of this rule is.
    /// </summary>
    public RequirementLevel RequirementLevel { get; set; }
}
