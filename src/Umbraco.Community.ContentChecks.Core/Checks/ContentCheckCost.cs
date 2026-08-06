namespace Umbraco.Community.ContentChecks.Core.Checks;

/// <summary>
/// Whether running a check costs money or a rate-limited call, independent of whether its
/// output is reproducible (see <see cref="ContentCheckKind"/>). This is what the runner
/// orders and short-circuits on.
/// </summary>
public enum ContentCheckCost
{
    /// <summary>
    /// The check runs locally with no metered cost (e.g. a required-property check).
    /// </summary>
    Free,

    /// <summary>
    /// The check makes a metered call (an AI judge, a third-party API).
    /// </summary>
    Metered,
}
