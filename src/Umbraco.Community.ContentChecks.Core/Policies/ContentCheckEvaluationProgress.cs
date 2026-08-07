namespace Umbraco.Community.ContentChecks.Core.Policies;

/// <summary>
/// Reports how many resolved targets (the root item, plus each nested element with a matching
/// rule) have finished, as they finish — coarse-grained on purpose, since finer-than-one-target
/// progress would mean changing the Phase 1 runner, which stays exactly as scoped.
/// </summary>
public sealed class ContentCheckEvaluationProgress
{
    /// <summary>
    /// The total number of resolved targets this evaluation will run.
    /// </summary>
    public required int TargetsTotal { get; init; }

    /// <summary>
    /// How many of those targets have finished running so far.
    /// </summary>
    public required int TargetsCompleted { get; init; }
}
