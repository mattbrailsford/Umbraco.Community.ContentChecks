using System.Text.Json;
using Umbraco.Community.ContentChecks.Core.Checks;
using Umbraco.Community.ContentChecks.Core.Policies;

namespace Umbraco.Community.ContentChecks.Web.Api.Management.Policies.Models;

/// <summary>
/// One rule within a policy — shared shape for both request and response, since a rule has no
/// server-generated fields (unlike the policy itself, which has a <c>Key</c>).
/// </summary>
public sealed class PolicyRuleModel
{
    /// <summary>
    /// The alias of the code-registered check this rule runs.
    /// </summary>
    public required string CheckAlias { get; init; }

    /// <summary>
    /// The check's configuration for this rule, or <c>null</c> if it takes none.
    /// </summary>
    public JsonElement? Config { get; init; }

    /// <summary>
    /// Whether this rule only advises, or blocks a gated operation on <c>Fail</c>.
    /// </summary>
    public ContentCheckRuleMode Mode { get; init; } = ContentCheckRuleMode.Advisory;

    /// <summary>
    /// How a <c>NotEvaluated</c> result from this rule should be treated by a gate.
    /// </summary>
    public ContentCheckNotEvaluatedPolicy NotEvaluatedPolicy { get; init; } = ContentCheckNotEvaluatedPolicy.FailOpen;
}
