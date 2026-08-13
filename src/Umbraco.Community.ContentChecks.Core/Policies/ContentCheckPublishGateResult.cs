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
    public required IReadOnlyList<ContentCheckPublishGateBlockingEntry> BlockingEntries { get; init; }

    /// <summary>
    /// Whether the publish should be rejected.
    /// </summary>
    public bool IsBlocked => BlockingEntries.Count > 0;
}

/// <summary>
/// One entry that blocks the current publishing user, with enough context to build a
/// human-readable cancellation reason.
/// </summary>
public sealed class ContentCheckPublishGateBlockingEntry
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
    /// evaluated" message if it's blocking by way of <see cref="ContentCheckNotEvaluatedPolicy.FailClosed"/>.
    /// </summary>
    public required string Message { get; init; }
}
