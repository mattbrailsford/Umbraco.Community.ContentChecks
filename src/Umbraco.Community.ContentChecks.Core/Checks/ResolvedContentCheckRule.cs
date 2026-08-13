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
    /// The resolved response for a failure of this rule, for the user and location this rule was
    /// resolved against. Defaults to <see cref="ContentCheckResponse.Warn"/> — the safe default
    /// when no Guard applies. See docs/guard-spec.md.
    /// </summary>
    public ContentCheckResponse Response { get; init; } = ContentCheckResponse.Warn;
}
