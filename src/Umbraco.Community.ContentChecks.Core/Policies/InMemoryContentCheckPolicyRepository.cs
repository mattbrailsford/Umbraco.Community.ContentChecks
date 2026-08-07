using System.Collections.Concurrent;

namespace Umbraco.Community.ContentChecks.Core.Policies;

/// <summary>
/// The zero-configuration default <see cref="IContentCheckPolicyRepository"/> — keeps the package
/// usable with no persistence set up. A real persistence layer overrides this registration.
/// </summary>
internal sealed class InMemoryContentCheckPolicyRepository : IContentCheckPolicyRepository
{
    private readonly ConcurrentDictionary<Guid, ContentCheckPolicy> _policies = new();
    private readonly ConcurrentDictionary<Guid, ContentCheckPolicyFolder> _folders = new();

    /// <inheritdoc />
    public Task<ContentCheckPolicy?> GetByKeyAsync(Guid key, CancellationToken cancellationToken = default)
        => Task.FromResult(_policies.GetValueOrDefault(key));

    /// <inheritdoc />
    public Task<ContentCheckPolicy?> GetByAliasAsync(string alias, CancellationToken cancellationToken = default)
        => Task.FromResult(_policies.Values.FirstOrDefault(p => p.Alias == alias));

    /// <inheritdoc />
    public Task<IReadOnlyList<ContentCheckPolicy>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ContentCheckPolicy>>(_policies.Values.ToList());

    /// <inheritdoc />
    public Task<IReadOnlyList<ContentCheckPolicy>> GetByContentTypeKeysAsync(IReadOnlyCollection<Guid> contentTypeKeys, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ContentCheckPolicy>>(
            _policies.Values.Where(p => contentTypeKeys.Contains(p.ContentTypeKey)).ToList());

    /// <inheritdoc />
    public Task<ContentCheckPolicy> SaveAsync(ContentCheckPolicy policy, CancellationToken cancellationToken = default)
    {
        _policies[policy.Key] = policy;
        return Task.FromResult(policy);
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(Guid key, CancellationToken cancellationToken = default)
        => Task.FromResult(_policies.TryRemove(key, out _));

    /// <inheritdoc />
    public Task<bool> PolicyExistsInFolderAsync(Guid folderKey, CancellationToken cancellationToken = default)
        => Task.FromResult(_policies.Values.Any(p => p.ParentFolderKey == folderKey));

    /// <inheritdoc />
    public Task<ContentCheckPolicyFolder?> GetFolderByKeyAsync(Guid key, CancellationToken cancellationToken = default)
        => Task.FromResult(_folders.GetValueOrDefault(key));

    /// <inheritdoc />
    public Task<IReadOnlyList<ContentCheckPolicyFolder>> GetAllFoldersAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ContentCheckPolicyFolder>>(_folders.Values.ToList());

    /// <inheritdoc />
    public Task<ContentCheckPolicyFolder> SaveFolderAsync(ContentCheckPolicyFolder folder, CancellationToken cancellationToken = default)
    {
        _folders[folder.Key] = folder;
        return Task.FromResult(folder);
    }

    /// <inheritdoc />
    public Task<bool> DeleteFolderAsync(Guid key, CancellationToken cancellationToken = default)
        => Task.FromResult(_folders.TryRemove(key, out _));

    /// <inheritdoc />
    public Task<bool> FolderExistsInFolderAsync(Guid folderKey, CancellationToken cancellationToken = default)
        => Task.FromResult(_folders.Values.Any(f => f.ParentFolderKey == folderKey));
}
