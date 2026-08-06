namespace Umbraco.Community.ContentChecks.Core.Checks;

/// <summary>
/// Why a check reported <see cref="ContentCheckStatus.NotEvaluated"/> instead of a real result.
/// </summary>
public enum ContentCheckNotEvaluatedReason
{
    /// <summary>
    /// The runner short-circuited this rule because a <c>Blocking</c> <c>Free</c> rule already
    /// failed — no expensive call was made.
    /// </summary>
    ShortCircuited,

    /// <summary>
    /// The check threw or a dependency it called (a provider, a third-party service) failed.
    /// </summary>
    Error,
}
