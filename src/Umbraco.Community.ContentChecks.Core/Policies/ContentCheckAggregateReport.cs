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
    /// Every entry from every resolved target, flattened into one list.
    /// </summary>
    public required IReadOnlyList<ContentCheckAggregateReportEntry> Entries { get; init; }

    /// <summary>
    /// The worst status across every entry, everywhere — for a results-panel badge. Answers "how
    /// bad is it," a genuinely different question from <see cref="HasBlockingFailure"/>'s "should
    /// this block the publish" — a page can have a real, failing check that's only
    /// <see cref="ContentCheckRuleMode.Advisory"/>, worth showing red here but nothing a gate
    /// should stop on.
    /// </summary>
    public ContentCheckStatus OverallStatus => Entries.Count == 0
        ? ContentCheckStatus.NotApplicable
        : Entries.Select(e => e.Entry.Result.Status).Min(StatusSeverityComparer.Instance);

    /// <summary>
    /// Whether any entry is a <see cref="ContentCheckRuleMode.Blocking"/> rule that failed. The
    /// exact condition the Phase 1 runner already computes internally to short-circuit metered
    /// checks, just widened from "free rules only" to every rule in every snapshot now that this
    /// report has all of them.
    /// </summary>
    public bool HasBlockingFailure => Entries.Any(e =>
        e.Entry.Mode == ContentCheckRuleMode.Blocking && e.Entry.Result.Status == ContentCheckStatus.Fail);

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
