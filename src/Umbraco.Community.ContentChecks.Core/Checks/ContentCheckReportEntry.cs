namespace Umbraco.Community.ContentChecks.Core.Checks;

/// <summary>
/// A log row describing what happened when one rule ran. Not the same concept as the rule
/// itself — a report is a list of entries, so this keeps "Entry" rather than picking up the
/// "Rule" naming.
/// </summary>
public sealed class ContentCheckReportEntry
{
    /// <summary>
    /// The alias of the check that ran.
    /// </summary>
    public required string CheckAlias { get; init; }

    /// <summary>
    /// <see cref="ResolvedContentCheckRule.RuleKey"/> for the rule that produced this entry —
    /// distinguishes two rules that both ran <see cref="CheckAlias"/> at the same location with
    /// different config (e.g. two "required property" rules checking different properties).
    /// </summary>
    public required Guid RuleKey { get; init; }

    /// <summary>
    /// How important this rule's failure is.
    /// </summary>
    public required RequirementLevel RequirementLevel { get; init; }

    /// <summary>
    /// How a <see cref="ContentCheckStatus.NotEvaluated"/> result from this rule should be treated
    /// by a gate — see docs/guard-spec.md.
    /// </summary>
    public Policies.ContentCheckNotEvaluatedPolicy NotEvaluatedPolicy { get; init; } = Policies.ContentCheckNotEvaluatedPolicy.FailOpen;

    /// <summary>
    /// The result the check produced.
    /// </summary>
    public required ContentCheckResult Result { get; init; }

    /// <summary>
    /// How long the check took to run.
    /// </summary>
    public TimeSpan Elapsed { get; init; }
}
