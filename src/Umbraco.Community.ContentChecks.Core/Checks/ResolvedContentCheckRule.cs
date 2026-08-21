namespace Umbraco.Community.ContentChecks.Core.Checks;

/// <summary>
/// The runtime-resolved counterpart of a persisted policy rule — same concept, one lifecycle
/// stage later: the check alias has been resolved to a real <see cref="IContentCheck"/> instance.
/// </summary>
public sealed class ResolvedContentCheckRule
{
    /// <summary>
    /// The persisted rule's own durable identity — see <see cref="Policies.ContentCheckPolicyRule.Key"/>.
    /// Distinguishes two rules that run the same <see cref="Check"/> at the same location with
    /// different <see cref="Config"/> (e.g. two "required property" rules checking different
    /// properties), which <see cref="Check"/>'s alias plus a location path alone can't tell apart.
    /// </summary>
    public required Guid RuleKey { get; init; }

    /// <summary>
    /// The check to run.
    /// </summary>
    public required IContentCheck Check { get; init; }

    /// <summary>
    /// The rule's own display title, if an editor gave it one — see
    /// <see cref="Policies.ContentCheckPolicyRule.Name"/>. <c>null</c> means callers should fall
    /// back to <see cref="IContentCheck.Name"/>.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// The rule's configuration for this check.
    /// </summary>
    public required ContentCheckConfig Config { get; init; }

    /// <summary>
    /// How important a failure of this rule is. Defaults to <see cref="RequirementLevel.Recommended"/>.
    /// </summary>
    public RequirementLevel RequirementLevel { get; init; } = RequirementLevel.Recommended;

    /// <summary>
    /// How a <see cref="ContentCheckStatus.NotEvaluated"/> result from this rule should be treated
    /// by a gate — see docs/guard-spec.md. Carried through to <see cref="ContentCheckReportEntry"/>
    /// so it's still available once a run's report is stored, not resolved again at gate time.
    /// </summary>
    public Policies.ContentCheckNotEvaluatedPolicy NotEvaluatedPolicy { get; init; } = Policies.ContentCheckNotEvaluatedPolicy.FailOpen;
}
