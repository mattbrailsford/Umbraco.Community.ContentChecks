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
    /// Whether a failure of this rule blocks. Defaults to <see cref="ContentCheckRuleMode.Advisory"/>.
    /// </summary>
    public ContentCheckRuleMode Mode { get; init; } = ContentCheckRuleMode.Advisory;
}
