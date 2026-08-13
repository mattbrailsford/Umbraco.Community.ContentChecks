using Umbraco.Community.ContentChecks.Core.Checks;

namespace Umbraco.Community.ContentChecks.Core.Policies;

/// <summary>
/// The full outcome of <see cref="IContentCheckEvaluationService.EvaluateAsync"/> for one content
/// item — every report entry from every resolved target (the root item, plus each nested element),
/// each stamped with the location it ran against.
/// </summary>
public sealed class ContentCheckAggregateReport
{
    /// <summary>
    /// The content item this report was evaluated for.
    /// </summary>
    public required Guid ContentKey { get; init; }

    /// <summary>
    /// The root content item's <see cref="IContentCheckSnapshot.ContentStamp"/> as of this run —
    /// <c>null</c> if the root snapshot couldn't provide one. A nested element has no stamp of its
    /// own worth tracking here: it's stored as part of the root item's own property values, so any
    /// change to it already moves the root's own stamp too. This is what lets a later reader (a
    /// results panel, or the future publish gate) tell whether this report still reflects the
    /// content as it currently stands, or is stale from before an edit.
    /// </summary>
    public string? ContentStamp { get; init; }

    /// <summary>
    /// Every entry from every resolved target, flattened into one list.
    /// </summary>
    public required IReadOnlyList<ContentCheckAggregateReportEntry> Entries { get; init; }

    /// <summary>
    /// The worst status across every entry, everywhere — for a results-panel badge. A report has
    /// no fixed answer to "does this block anyone" — that depends on who's asking, decided fresh at
    /// publish time by a Guard (see docs/guard-spec.md), never baked into a stored run.
    /// </summary>
    public ContentCheckStatus OverallStatus => Entries.Count == 0
        ? ContentCheckStatus.NotApplicable
        : Entries.Select(e => e.Entry.Result.Status).Min(StatusSeverityComparer.Instance);

    /// <summary>
    /// Orders <see cref="ContentCheckStatus"/> by severity — <c>Fail &gt; Warn &gt; NotEvaluated &gt;
    /// Pass &gt; NotApplicable</c> — since the five values aren't a simple good/bad scale.
    /// <see cref="ContentCheckStatus.NotApplicable"/> sits lowest deliberately: it means the check
    /// genuinely didn't apply, not that something's wrong, so a report that's entirely
    /// <see cref="ContentCheckStatus.NotApplicable"/> reads as "nothing here to check," not "fine."
    /// </summary>
    private sealed class StatusSeverityComparer : IComparer<ContentCheckStatus>
    {
        public static readonly StatusSeverityComparer Instance = new();

        private static readonly ContentCheckStatus[] SeverityOrder =
        [
            ContentCheckStatus.Fail,
            ContentCheckStatus.Warn,
            ContentCheckStatus.NotEvaluated,
            ContentCheckStatus.Pass,
            ContentCheckStatus.NotApplicable,
        ];

        public int Compare(ContentCheckStatus x, ContentCheckStatus y)
            => Array.IndexOf(SeverityOrder, x).CompareTo(Array.IndexOf(SeverityOrder, y));
    }
}
