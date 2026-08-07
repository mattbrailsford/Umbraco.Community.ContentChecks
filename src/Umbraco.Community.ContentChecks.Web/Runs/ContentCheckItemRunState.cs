using Umbraco.Community.ContentChecks.Core.Policies;

namespace Umbraco.Community.ContentChecks.Web.Runs;

/// <summary>
/// One content item's own progress within a <see cref="ContentCheckRunState"/>. Mutated in place
/// by the background worker as the item's evaluation proceeds — <see cref="Progress"/> updates
/// as targets finish, then exactly one of <see cref="Report"/> or <see cref="Error"/> is set once.
/// </summary>
public sealed class ContentCheckItemRunState
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentCheckItemRunState"/> class.
    /// </summary>
    /// <param name="contentKey">The content item this state tracks.</param>
    public ContentCheckItemRunState(Guid contentKey)
    {
        ContentKey = contentKey;
    }

    /// <summary>
    /// The content item this state tracks.
    /// </summary>
    public Guid ContentKey { get; }

    /// <summary>
    /// How many of this item's resolved targets have finished, as of the last update.
    /// <c>null</c> until the first one reports.
    /// </summary>
    public ContentCheckEvaluationProgress? Progress { get; internal set; }

    /// <summary>
    /// The item's finished report. <c>null</c> until evaluation completes successfully.
    /// </summary>
    public ContentCheckAggregateReport? Report { get; internal set; }

    /// <summary>
    /// Why this item failed — the content couldn't be found, or evaluation threw. <c>null</c>
    /// unless it did.
    /// </summary>
    public string? Error { get; internal set; }

    /// <summary>
    /// Whether this item has finished, successfully or not.
    /// </summary>
    public bool IsFinished => Report is not null || Error is not null;
}
