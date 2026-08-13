namespace Umbraco.Community.ContentChecks.Core.Policies;

/// <summary>
/// The outcome of resolving Guard, for one publishing user, against a content item's stored
/// check report — see docs/guard-spec.md's "How the two combine" section.
/// </summary>
public sealed class ContentCheckPublishGateResult
{
    /// <summary>
    /// Every entry that counts as failing (a real <c>Fail</c>, or a <c>NotEvaluated</c> entry
    /// whose rule is <see cref="ContentCheckNotEvaluatedPolicy.FailClosed"/>) and resolves to
    /// <see cref="Checks.ContentCheckResponse.Block"/> for this user.
    /// </summary>
    public required IReadOnlyList<ContentCheckPublishGateEntry> BlockingEntries { get; init; }

    /// <summary>
    /// Every entry that counts as failing at <see cref="Checks.RequirementLevel.Recommended"/> —
    /// independent of Guard, since a Recommended-level miss is a fact about the content, not an
    /// enforcement decision. A publish that isn't blocked can still have these; the caller (the
    /// publish-gate notification handler) decides how a block and a recommendation notice combine
    /// into a single toast — a block supersedes a recommendation notice, never both at once.
    /// </summary>
    public required IReadOnlyList<ContentCheckPublishGateEntry> RecommendedMissedEntries { get; init; }

    /// <summary>
    /// Whether the publish should be rejected.
    /// </summary>
    public bool IsBlocked => BlockingEntries.Count > 0;
}

/// <summary>
/// One entry the publish gate cares about — blocking, or a non-blocking Recommended-level miss —
/// with enough context for a caller to build its own message.
/// </summary>
public sealed class ContentCheckPublishGateEntry
{
    /// <summary>
    /// The alias of the check that produced this entry.
    /// </summary>
    public required string CheckAlias { get; init; }

    /// <summary>
    /// How important this rule's failure is.
    /// </summary>
    public required Checks.RequirementLevel RequirementLevel { get; init; }

    /// <summary>
    /// The check's own message — the failure reason if it actually failed, or the "why it wasn't
    /// evaluated" message if it's counted by way of <see cref="ContentCheckNotEvaluatedPolicy.FailClosed"/>.
    /// </summary>
    public required string Message { get; init; }
}
