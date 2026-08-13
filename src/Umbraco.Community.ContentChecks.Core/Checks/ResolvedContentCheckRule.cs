namespace Umbraco.Community.ContentChecks.Core.Checks;

/// <summary>
/// The runtime-resolved counterpart of a persisted policy rule — same concept, one lifecycle
/// stage later: the check alias has been resolved to a real <see cref="IContentCheck"/> instance.
/// </summary>
public sealed class ResolvedContentCheckRule
{
    /// <summary>
    /// The check to run.
    /// </summary>
    public required IContentCheck Check { get; init; }

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
