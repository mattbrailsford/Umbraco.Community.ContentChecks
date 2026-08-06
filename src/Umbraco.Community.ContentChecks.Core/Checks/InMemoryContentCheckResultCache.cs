using System.Collections.Concurrent;

namespace Umbraco.Community.ContentChecks.Core.Checks;

/// <summary>
/// In-memory v1 of <see cref="IContentCheckResultCache"/>. No eviction beyond process lifetime —
/// fine for v1, since the cache key already includes everything that would invalidate an entry
/// (content version, check assembly version, resolved config).
/// </summary>
internal sealed class InMemoryContentCheckResultCache : IContentCheckResultCache
{
    private readonly ConcurrentDictionary<string, ContentCheckResult> _entries = new();

    /// <inheritdoc />
    public Task<ContentCheckResult?> GetAsync(string key, CancellationToken cancellationToken)
        => Task.FromResult(_entries.TryGetValue(key, out var result) ? result : null);

    /// <inheritdoc />
    public Task SetAsync(string key, ContentCheckResult result, CancellationToken cancellationToken)
    {
        _entries[key] = result;
        return Task.CompletedTask;
    }
}
