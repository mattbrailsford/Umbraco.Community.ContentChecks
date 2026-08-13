namespace Umbraco.Community.ContentChecks.Core.Checks;

/// <summary>
/// Read/write access to <see cref="ContentCheckStoredResult"/> — the last completed run's results
/// per content item, so a results panel can show them without needing a run currently in flight.
/// Only the latest run is kept per content item today; see
/// <see cref="ReplaceForContentAsync"/>.
/// </summary>
public interface IContentCheckResultService
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
    /// Atomically replaces every stored result for a content item with a new run's results.
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
