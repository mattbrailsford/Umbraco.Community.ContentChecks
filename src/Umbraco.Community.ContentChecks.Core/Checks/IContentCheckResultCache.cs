namespace Umbraco.Community.ContentChecks.Core.Checks;

/// <summary>
/// Caches a check's result for a given content version, so re-running an unchanged item skips
/// the check entirely (including a <c>Metered</c>/AI-judged call). The runner is the only
/// caller; check authors never see this directly.
/// </summary>
public interface IContentCheckResultCache
{
    /// <summary>
    /// Looks up a cached result.
    /// </summary>
    /// <param name="key">The cache key, built by the runner.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The cached result, or <c>null</c> if there isn't one.</returns>
    Task<ContentCheckResult?> GetAsync(string key, CancellationToken cancellationToken);

    /// <summary>
    /// Stores a result.
    /// </summary>
    /// <param name="key">The cache key, built by the runner.</param>
    /// <param name="result">The result to store.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetAsync(string key, ContentCheckResult result, CancellationToken cancellationToken);
}
