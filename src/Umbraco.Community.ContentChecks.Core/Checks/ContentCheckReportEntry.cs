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
    /// Whether this rule's failure blocks.
    /// </summary>
    public required ContentCheckRuleMode Mode { get; init; }

    /// <summary>
    /// The result the check produced.
    /// </summary>
    public required ContentCheckResult Result { get; init; }

    /// <summary>
    /// How long the check took to run.
    /// </summary>
    public TimeSpan Elapsed { get; init; }
}
