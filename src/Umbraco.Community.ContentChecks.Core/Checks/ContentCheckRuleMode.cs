namespace Umbraco.Community.ContentChecks.Core.Checks;

/// <summary>
/// Whether a rule's failure is a hard block or just a warning.
/// </summary>
public enum ContentCheckRuleMode
{
    /// <summary>
    /// A failure is shown but doesn't block anything.
    /// </summary>
    Advisory,

    /// <summary>
    /// A failure blocks the action that triggered the check (e.g. publish).
    /// </summary>
    Blocking,
}
