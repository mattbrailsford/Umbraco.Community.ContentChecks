using Umbraco.Community.ContentChecks.Core.Checks;

namespace Umbraco.Community.ContentChecks.Core.Policies;

/// <summary>
/// One persisted rule within a <see cref="ContentCheckPolicy"/> — a check alias plus how it's
/// configured and how strictly it's enforced. The runtime-resolved counterpart, once the check
/// alias has been resolved to a real <see cref="IContentCheck"/> instance, is
/// <see cref="ResolvedContentCheckRule"/>.
/// </summary>
public sealed class ContentCheckPolicyRule
{
    /// <summary>
    /// The alias of the code-registered <see cref="IContentCheck"/> this rule runs.
    /// </summary>
    public required string CheckAlias { get; set; }

    /// <summary>
    /// The check's configuration for this rule.
    /// </summary>
    public ContentCheckConfig Config { get; set; } = new();

    /// <summary>
    /// Whether this rule only advises, or blocks a gated operation on <c>Fail</c>.
    /// </summary>
    public ContentCheckRuleMode Mode { get; set; } = ContentCheckRuleMode.Advisory;

    /// <summary>
    /// How a <c>NotEvaluated</c> result from this rule should be treated by a gate.
    /// </summary>
    public ContentCheckNotEvaluatedPolicy NotEvaluatedPolicy { get; set; } = ContentCheckNotEvaluatedPolicy.FailOpen;
}
