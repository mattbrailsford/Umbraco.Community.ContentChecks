using Umbraco.Community.ContentChecks.Core.Checks;

namespace Umbraco.Community.ContentChecks.Core.Policies;

/// <summary>
/// One persisted rule within a <see cref="ContentCheckPolicy"/> — a check alias plus how it's
/// configured and how strictly it's enforced. The runtime-resolved counterpart, once the check
/// alias has been resolved to a real <see cref="IContentCheck"/> instance, is
/// <see cref="ResolvedContentCheckRule"/>.
/// </summary>
public sealed class ContentCheckPolicyRule
{
    /// <summary>
    /// The rule's own durable identity, distinct from <see cref="ContentCheckPolicy.Key"/> —
    /// needed because a policy can carry more than one rule for the same
    /// <see cref="CheckAlias"/> (e.g. two "required property" rules, each checking a different
    /// property). Without it, two such rules are indistinguishable downstream once results are
    /// only identified by check alias and location. A rule read from before this field existed
    /// deserializes to <see cref="Guid.Empty"/> — the persistence layer backfills a fresh value
    /// for those on read so every rule ends up with a real identity.
    /// </summary>
    public Guid Key { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The alias of the code-registered <see cref="IContentCheck"/> this rule runs.
    /// </summary>
    public required string CheckAlias { get; set; }

    /// <summary>
    /// The check's configuration for this rule.
    /// </summary>
    public ContentCheckConfig Config { get; set; } = new();

    /// <summary>
    /// How important a failure of this rule is. Doesn't decide whether it blocks anything — see
    /// <see cref="RequirementLevel"/> and docs/guard-spec.md.
    /// </summary>
    public RequirementLevel RequirementLevel { get; set; } = RequirementLevel.Recommended;

    /// <summary>
    /// How a <c>NotEvaluated</c> result from this rule should be treated by a gate.
    /// </summary>
    public ContentCheckNotEvaluatedPolicy NotEvaluatedPolicy { get; set; } = ContentCheckNotEvaluatedPolicy.FailOpen;
}
