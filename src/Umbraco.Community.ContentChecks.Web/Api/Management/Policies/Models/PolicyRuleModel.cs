using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Umbraco.Community.ContentChecks.Core.Checks;
using Umbraco.Community.ContentChecks.Core.Policies;

namespace Umbraco.Community.ContentChecks.Web.Api.Management.Policies.Models;

/// <summary>
/// One rule within a policy — shared shape for both request and response, since a rule has no
/// server-generated fields (unlike the policy itself, which has a <c>Key</c>).
/// </summary>
public class PolicyRuleModel
{
    /// <summary>
    /// The alias of the code-registered check this rule runs.
    /// </summary>
    [Required]
    public string CheckAlias { get; set; } = string.Empty;

    /// <summary>
    /// The check's configuration for this rule, or <c>null</c> if it takes none.
    /// </summary>
    public JsonElement? Config { get; set; }

    /// <summary>
    /// How important a failure of this rule is. Doesn't decide whether it blocks anything — see
    /// docs/guard-spec.md.
    /// </summary>
    public RequirementLevel RequirementLevel { get; set; } = RequirementLevel.Recommended;

    /// <summary>
    /// How a <c>NotEvaluated</c> result from this rule should be treated by a gate.
    /// </summary>
    public ContentCheckNotEvaluatedPolicy NotEvaluatedPolicy { get; set; } = ContentCheckNotEvaluatedPolicy.FailOpen;
}
