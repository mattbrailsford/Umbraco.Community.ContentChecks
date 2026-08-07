using System.Collections.Concurrent;
using Umbraco.Community.ContentChecks.Core.Policies;

namespace Umbraco.Community.ContentChecks.Web.Runs;

/// <summary>
/// The live state of one evaluation run — one or more content items, each tracked independently.
/// Held entirely in memory by <see cref="IContentCheckRunTracker"/>; a run still going when the
/// app pool recycles is simply lost, no run-history feature is implied by this.
/// </summary>
public sealed class ContentCheckRunState
{
    private readonly ConcurrentDictionary<Guid, ContentCheckItemRunState> _items;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentCheckRunState"/> class.
    /// </summary>
    /// <param name="runId">The run's own identity.</param>
    /// <param name="contentKeys">The content items this run evaluates.</param>
    public ContentCheckRunState(Guid runId, IReadOnlyList<Guid> contentKeys)
    {
        RunId = runId;
        ContentKeys = contentKeys;
        _items = new ConcurrentDictionary<Guid, ContentCheckItemRunState>(
            contentKeys.Select(key => new KeyValuePair<Guid, ContentCheckItemRunState>(key, new ContentCheckItemRunState(key))));
    }

    /// <summary>
    /// The run's own identity.
    /// </summary>
    public Guid RunId { get; }

    /// <summary>
    /// The content items this run evaluates — a single key is just the N=1 case, not a separate
    /// mechanism.
    /// </summary>
    public IReadOnlyList<Guid> ContentKeys { get; }

    /// <summary>
    /// Every item's own state, keyed by content key.
    /// </summary>
    public IReadOnlyDictionary<Guid, ContentCheckItemRunState> Items => _items;

    /// <summary>
    /// <see cref="ContentCheckRunStatus.Completed"/> once every item has finished, whether that's
    /// a real report or a failure — never stored independently, so there's nothing to keep in sync.
    /// </summary>
    public ContentCheckRunStatus Status => _items.Values.All(i => i.IsFinished)
        ? ContentCheckRunStatus.Completed
        : ContentCheckRunStatus.Running;

    /// <summary>
    /// Records a content item's latest progress.
    /// </summary>
    /// <param name="contentKey">The content item that reported progress.</param>
    /// <param name="progress">Its latest progress.</param>
    internal void UpdateProgress(Guid contentKey, ContentCheckEvaluationProgress progress)
    {
        if (_items.TryGetValue(contentKey, out var item))
        {
            item.Progress = progress;
        }
    }

    /// <summary>
    /// Records a content item's finished report.
    /// </summary>
    /// <param name="contentKey">The content item that finished.</param>
    /// <param name="report">Its finished report.</param>
    internal void CompleteItem(Guid contentKey, ContentCheckAggregateReport report)
    {
        if (_items.TryGetValue(contentKey, out var item))
        {
            item.Report = report;
        }
    }

    /// <summary>
    /// Records a content item's failure — the content couldn't be found, or evaluation threw.
    /// </summary>
    /// <param name="contentKey">The content item that failed.</param>
    /// <param name="error">Why it failed.</param>
    internal void FailItem(Guid contentKey, string error)
    {
        if (_items.TryGetValue(contentKey, out var item))
        {
            item.Error = error;
        }
    }
}
