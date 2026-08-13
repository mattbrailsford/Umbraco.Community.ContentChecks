namespace Umbraco.Community.ContentChecks.Core.Checks;

/// <summary>
/// What happens when a rule fails, for the person and location it's resolved against — the
/// output of a Guard lookup for a given <see cref="RequirementLevel"/>, never
/// authored directly on the rule. See docs/guard-spec.md.
/// </summary>
public enum ContentCheckResponse
{
    /// <summary>
    /// A failure is shown but doesn't block anything.
    /// </summary>
    Warn,

    /// <summary>
    /// A failure blocks the action that triggered the check (e.g. publish).
    /// </summary>
    Block,
}
