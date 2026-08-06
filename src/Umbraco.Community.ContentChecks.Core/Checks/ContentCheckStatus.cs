namespace Umbraco.Community.ContentChecks.Core.Checks;

/// <summary>
/// The result of evaluating one check against one target. Modelled on Umbraco's own
/// <c>HealthCheckStatus.ResultType</c>, extended with the two states site health doesn't need
/// but content quality does (<see cref="NotApplicable"/>, <see cref="NotEvaluated"/>).
/// </summary>
public enum ContentCheckStatus
{
    /// <summary>
    /// The check passed.
    /// </summary>
    Pass,

    /// <summary>
    /// The check found an issue worth flagging, but not blocking.
    /// </summary>
    Warning,

    /// <summary>
    /// The check found an issue.
    /// </summary>
    Fail,

    /// <summary>
    /// The check does not apply to this content (e.g. the property it targets doesn't exist on
    /// this content type).
    /// </summary>
    NotApplicable,

    /// <summary>
    /// The check did not run. See <see cref="ContentCheckNotEvaluatedReason"/> for why.
    /// </summary>
    NotEvaluated,
}
