using Umbraco.Community.ContentChecks.Core.Policies;

namespace Umbraco.Community.ContentChecks.Tests.Unit.Policies;

/// <summary>
/// An in-memory <see cref="IContentCheckPolicyService"/> test double -- exercises the same
/// "policies whose ContentTypeKey is one of these keys" filtering the real repository does,
/// without needing a database.
/// </summary>
internal sealed class FakeContentCheckPolicyService : IContentCheckPolicyService
{
    private readonly List<ContentCheckPolicy> _policies = [];
    private readonly List<ContentCheckPolicyFolder> _folders = [];

    public void AddPolicy(ContentCheckPolicy policy) => _policies.Add(policy);

    public Task<ContentCheckPolicy?> GetPolicyAsync(Guid key, CancellationToken cancellationToken = default)
        => Task.FromResult(_policies.FirstOrDefault(p => p.Key == key));

    public Task<ContentCheckPolicy?> GetPolicyByAliasAsync(string alias, CancellationToken cancellationToken = default)
        => Task.FromResult(_policies.FirstOrDefault(p => p.Alias == alias));

    public Task<IReadOnlyList<ContentCheckPolicy>> GetAllPoliciesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ContentCheckPolicy>>(_policies.ToList());

    public Task<IReadOnlyList<ContentCheckPolicy>> GetPoliciesByContentTypeKeysAsync(IReadOnlyCollection<Guid> contentTypeKeys, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ContentCheckPolicy>>(_policies.Where(p => contentTypeKeys.Contains(p.ContentTypeKey)).ToList());

    public Task<ContentCheckPolicy> SavePolicyAsync(ContentCheckPolicy policy, CancellationToken cancellationToken = default)
    {
        _policies.RemoveAll(p => p.Key == policy.Key);
        _policies.Add(policy);
        return Task.FromResult(policy);
    }

    public Task<bool> DeletePolicyAsync(Guid key, CancellationToken cancellationToken = default)
        => Task.FromResult(_policies.RemoveAll(p => p.Key == key) > 0);

    public Task<ContentCheckPolicyFolder?> GetFolderAsync(Guid key, CancellationToken cancellationToken = default)
        => Task.FromResult(_folders.FirstOrDefault(f => f.Key == key));

    public Task<IReadOnlyList<ContentCheckPolicyFolder>> GetAllFoldersAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ContentCheckPolicyFolder>>(_folders.ToList());

    public Task<ContentCheckPolicyFolder> SaveFolderAsync(ContentCheckPolicyFolder folder, CancellationToken cancellationToken = default)
    {
        _folders.RemoveAll(f => f.Key == folder.Key);
        _folders.Add(folder);
        return Task.FromResult(folder);
    }

    public Task<bool> DeleteFolderAsync(Guid key, CancellationToken cancellationToken = default)
        => Task.FromResult(_folders.RemoveAll(f => f.Key == key) > 0);

    public Task<bool> FolderHasChildrenAsync(Guid key, CancellationToken cancellationToken = default)
        => Task.FromResult(_policies.Any(p => p.ParentFolderKey == key) || _folders.Any(f => f.ParentFolderKey == key));
}
