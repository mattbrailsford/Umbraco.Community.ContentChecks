namespace Umbraco.Community.ContentChecks.Core.Policies;

/// <summary>
/// How a rule's <c>NotEvaluated</c> result should be treated by a caller gating on it (e.g. a
/// publish trigger). Lives on the persisted rule, not the runner (<see
/// cref="Umbraco.Community.ContentChecks.Core.Checks.IContentCheckRunner"/>) — the runner's job is
/// to run checks and report faithfully what happened, not to decide whether a gate should stop on it.
/// </summary>
public enum ContentCheckNotEvaluatedPolicy
{
    /// <summary>
    /// A <c>NotEvaluated</c> result does not block a gated operation. The default — an outage or a
    /// short-circuited check should not itself become the reason every publish fails.
    /// </summary>
    FailOpen,

    /// <summary>
    /// A <c>NotEvaluated</c> result is treated the same as a <c>Fail</c> by a gate.
    /// </summary>
    FailClosed,
}
