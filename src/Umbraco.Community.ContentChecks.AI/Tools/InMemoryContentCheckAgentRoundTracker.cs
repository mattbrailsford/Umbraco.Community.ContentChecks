using System.Collections.Concurrent;

namespace Umbraco.Community.ContentChecks.AI.Tools;

/// <summary>
/// In-memory <see cref="IContentCheckAgentRoundTracker"/> -- a process-lifetime counter, not
/// persisted. Good enough for a v1 hard cap: the goal is stopping a single runaway agent loop
/// within one app lifetime, not an audited history of every run.
/// </summary>
internal sealed class InMemoryContentCheckAgentRoundTracker : IContentCheckAgentRoundTracker
{
    /// <summary>
    /// The hard cap. Not yet configurable -- a fixed default is deliberately simpler than a
    /// setting nobody has asked to tune yet.
    /// </summary>
    public const int MaxRounds = 5;

    private readonly ConcurrentDictionary<string, int> _rounds = new();

    /// <inheritdoc />
    public ContentCheckAgentRoundStatus RecordRound(Guid contentKey, string? culture, string? segment)
    {
        var key = $"{contentKey:N}|{culture}|{segment}";
        var round = _rounds.AddOrUpdate(key, 1, static (_, existing) => existing + 1);

        return new ContentCheckAgentRoundStatus
        {
            Round = round,
            MaxRounds = MaxRounds,
        };
    }
}
