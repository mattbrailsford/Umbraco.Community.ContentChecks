namespace Umbraco.Community.ContentChecks.AI.Tools;

/// <summary>
/// Enforces the round cap an agent's fix-and-recheck loop must never exceed (finding G2,
/// docs/overview.md) -- settled as the tool layer's own hard limit, not a system-prompt hint an
/// agent could ignore (docs/ai-integration-spec.md's build checklist, open question 2). Tracked
/// per content item/variant rather than per conversation, since that's the unit a loop actually
/// iterates over.
/// </summary>
public interface IContentCheckAgentRoundTracker
{
    /// <summary>
    /// Records one round of checks against a content item/variant and reports whether the cap has
    /// been reached.
    /// </summary>
    /// <param name="contentKey">The content item's key.</param>
    /// <param name="culture">The culture the round ran for.</param>
    /// <param name="segment">The segment the round ran for.</param>
    /// <returns>The round number just recorded, and whether the cap is now reached.</returns>
    ContentCheckAgentRoundStatus RecordRound(Guid contentKey, string? culture, string? segment);
}

/// <summary>
/// The outcome of <see cref="IContentCheckAgentRoundTracker.RecordRound"/>.
/// </summary>
public sealed class ContentCheckAgentRoundStatus
{
    /// <summary>
    /// The 1-based round number just recorded for this content item/variant.
    /// </summary>
    public required int Round { get; init; }

    /// <summary>
    /// The maximum number of rounds allowed before the cap stops the loop.
    /// </summary>
    public required int MaxRounds { get; init; }

    /// <summary>
    /// Whether this round exceeds <see cref="MaxRounds"/> -- the caller must stop running checks
    /// and report to a human instead.
    /// </summary>
    public bool CapReached => Round > MaxRounds;
}
