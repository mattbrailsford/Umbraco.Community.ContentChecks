namespace Umbraco.Community.ContentChecks.Core.Checks;

/// <summary>
/// Persistence for <see cref="ContentCheckStoredResult"/> — the last completed run's results per
/// content item. Only the latest run is kept per <see cref="ContentCheckStoredResult.ContentKey"/>
/// (see <see cref="ReplaceForContentAsync"/>); nothing here reads or writes a run's live,
/// in-progress state — that's <c>IContentCheckRunTracker</c>'s job, in the Web project.
/// </summary>
internal interface IContentCheckResultRepository
{
    /// <summary>
    /// Gets every stored result for a content item, from its most recently completed run.
    /// </summary>
    /// <param name="contentKey">The content item's key.</param>
    /// <param name="culture">The culture the run was evaluated for.</param>
    /// <param name="segment">The segment the run was evaluated for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Every stored result, or empty if this item has never been evaluated.</returns>
    Task<IReadOnlyList<ContentCheckStoredResult>> GetByContentKeyAsync(
        Guid contentKey,
        string? culture = null,
        string? segment = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically replaces every stored result for a content item with a new run's results —
    /// the "clear before a run" requirement, applied at the moment new results are ready rather
    /// than before the run starts, so a run that fails partway through never leaves a content item
    /// with no results at all.
    /// </summary>
    /// <param name="contentKey">The content item's key.</param>
    /// <param name="culture">The culture the run was evaluated for.</param>
    /// <param name="segment">The segment the run was evaluated for.</param>
    /// <param name="results">The new results to store, replacing whatever was there before.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ReplaceForContentAsync(
        Guid contentKey,
        string? culture,
        string? segment,
        IReadOnlyList<ContentCheckStoredResult> results,
        CancellationToken cancellationToken = default);
}
