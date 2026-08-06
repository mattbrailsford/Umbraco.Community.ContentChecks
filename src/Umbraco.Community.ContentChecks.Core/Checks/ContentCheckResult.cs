namespace Umbraco.Community.ContentChecks.Core.Checks;

/// <summary>
/// What a check returns from evaluating one snapshot.
/// </summary>
public sealed class ContentCheckResult
{
    /// <summary>
    /// The outcome of the evaluation.
    /// </summary>
    public required ContentCheckStatus Status { get; init; }

    /// <summary>
    /// Human-readable feedback. The only place a suggested fix is expressed — as prose an editor
    /// or agent acts on themselves, through the normal editing pipeline. Checks never fix
    /// content; they only report.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// A continuous score, for checks that produce one (e.g. an AI-judged rubric). <c>null</c>
    /// for checks that only ever produce a discrete status.
    /// </summary>
    public double? Score { get; init; }

    /// <summary>
    /// The threshold the score was judged against, when <see cref="Score"/> is present. First-class
    /// so the results UI's score-vs-threshold bar doesn't need to parse per-check metadata.
    /// </summary>
    public double? Threshold { get; init; }

    /// <summary>
    /// Why the check did not run, when <see cref="Status"/> is <see cref="ContentCheckStatus.NotEvaluated"/>.
    /// </summary>
    public ContentCheckNotEvaluatedReason? NotEvaluatedReason { get; init; }

    /// <summary>
    /// Where on the snapshot this result applies. <c>null</c> or empty means the whole item.
    /// </summary>
    public IReadOnlyList<ContentCheckTarget>? Targets { get; init; }
}
