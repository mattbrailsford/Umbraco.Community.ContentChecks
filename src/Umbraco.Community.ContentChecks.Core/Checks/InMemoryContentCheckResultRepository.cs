using System.Collections.Concurrent;

namespace Umbraco.Community.ContentChecks.Core.Checks;

/// <summary>
/// The zero-configuration default <see cref="IContentCheckResultRepository"/> — keeps the package
/// usable with no persistence set up. Mirrors <see cref="InMemoryContentCheckPolicyRepository"/>'s
/// role for policies. A real persistence layer overrides this registration.
/// </summary>
internal sealed class InMemoryContentCheckResultRepository : IContentCheckResultRepository
{
    private readonly ConcurrentDictionary<Guid, IReadOnlyList<ContentCheckStoredResult>> _resultsByContentKey = new();

    /// <inheritdoc />
    public Task<IReadOnlyList<ContentCheckStoredResult>> GetByContentKeyAsync(
        Guid contentKey, string? culture = null, string? segment = null, CancellationToken cancellationToken = default)
    {
        if (!_resultsByContentKey.TryGetValue(contentKey, out var results))
        {
            return Task.FromResult<IReadOnlyList<ContentCheckStoredResult>>([]);
        }

        var filtered = results.Where(r => r.Culture == culture && r.Segment == segment).ToList();
        return Task.FromResult<IReadOnlyList<ContentCheckStoredResult>>(filtered);
    }

    /// <inheritdoc />
    public Task ReplaceForContentAsync(
        Guid contentKey,
        string? culture,
        string? segment,
        IReadOnlyList<ContentCheckStoredResult> results,
        CancellationToken cancellationToken = default)
    {
        _resultsByContentKey.AddOrUpdate(
            contentKey,
            _ => results,
            (_, existing) => existing.Where(r => r.Culture != culture || r.Segment != segment).Concat(results).ToList());

        return Task.CompletedTask;
    }
}
