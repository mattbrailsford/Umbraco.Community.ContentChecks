using Microsoft.EntityFrameworkCore;
using Umbraco.Cms.Persistence.EFCore.Scoping;
using Umbraco.Community.ContentChecks.Core.Policies;

namespace Umbraco.Community.ContentChecks.Persistence.Policies;

/// <summary>
/// EF Core implementation of <see cref="IContentCheckPolicyRepository"/>.
/// </summary>
internal sealed class EFCoreContentCheckPolicyRepository : IContentCheckPolicyRepository
{
    private readonly IEFCoreScopeProvider<ContentChecksDbContext> _scopeProvider;
    private readonly IContentCheckPolicyFactory _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="EFCoreContentCheckPolicyRepository"/> class.
    /// </summary>
    /// <param name="scopeProvider">The EF Core scope provider.</param>
    /// <param name="factory">Maps between domain models and entities.</param>
    public EFCoreContentCheckPolicyRepository(
        IEFCoreScopeProvider<ContentChecksDbContext> scopeProvider,
        IContentCheckPolicyFactory factory)
    {
        _scopeProvider = scopeProvider;
        _factory = factory;
    }

    /// <inheritdoc />
    public async Task<ContentCheckPolicy?> GetByKeyAsync(Guid key, CancellationToken cancellationToken = default)
    {
        using IEFCoreScope<ContentChecksDbContext> scope = _scopeProvider.CreateScope();
        var entity = await scope.ExecuteWithContextAsync(async db =>
            await db.Policies.FirstOrDefaultAsync(p => p.Key == key, cancellationToken));
        scope.Complete();
        return entity is null ? null : _factory.BuildDomain(entity);
    }

    /// <inheritdoc />
    public async Task<ContentCheckPolicy?> GetByAliasAsync(string alias, CancellationToken cancellationToken = default)
    {
        using IEFCoreScope<ContentChecksDbContext> scope = _scopeProvider.CreateScope();
        var entity = await scope.ExecuteWithContextAsync(async db =>
            await db.Policies.FirstOrDefaultAsync(p => p.Alias == alias, cancellationToken));
        scope.Complete();
        return entity is null ? null : _factory.BuildDomain(entity);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ContentCheckPolicy>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using IEFCoreScope<ContentChecksDbContext> scope = _scopeProvider.CreateScope();
        var entities = await scope.ExecuteWithContextAsync(async db => await db.Policies.ToListAsync(cancellationToken));
        scope.Complete();
        return entities.Select(_factory.BuildDomain).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ContentCheckPolicy>> GetByContentTypeKeysAsync(IReadOnlyCollection<Guid> contentTypeKeys, CancellationToken cancellationToken = default)
    {
        using IEFCoreScope<ContentChecksDbContext> scope = _scopeProvider.CreateScope();
        var entities = await scope.ExecuteWithContextAsync(async db =>
            await db.Policies.Where(p => contentTypeKeys.Contains(p.ContentTypeKey)).ToListAsync(cancellationToken));
        scope.Complete();
        return entities.Select(_factory.BuildDomain).ToList();
    }

    /// <inheritdoc />
    public async Task<ContentCheckPolicy> SaveAsync(ContentCheckPolicy policy, CancellationToken cancellationToken = default)
    {
        using IEFCoreScope<ContentChecksDbContext> scope = _scopeProvider.CreateScope();

        await scope.ExecuteWithContextAsync(async db =>
        {
            var existing = await db.Policies.FindAsync([policy.Key], cancellationToken);
            if (existing is null)
            {
                db.Policies.Add(_factory.BuildEntity(policy));
            }
            else
            {
                _factory.UpdateEntity(existing, policy);
            }

            await db.SaveChangesAsync(cancellationToken);
            return true;
        });

        scope.Complete();
        return policy;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid key, CancellationToken cancellationToken = default)
    {
        using IEFCoreScope<ContentChecksDbContext> scope = _scopeProvider.CreateScope();

        var deleted = await scope.ExecuteWithContextAsync(async db =>
        {
            var existing = await db.Policies.FindAsync([key], cancellationToken);
            if (existing is null)
            {
                return false;
            }

            db.Policies.Remove(existing);
            await db.SaveChangesAsync(cancellationToken);
            return true;
        });

        scope.Complete();
        return deleted;
    }

    /// <inheritdoc />
    public async Task<ContentCheckPolicyFolder?> GetFolderByKeyAsync(Guid key, CancellationToken cancellationToken = default)
    {
        using IEFCoreScope<ContentChecksDbContext> scope = _scopeProvider.CreateScope();
        var entity = await scope.ExecuteWithContextAsync(async db =>
            await db.PolicyFolders.FirstOrDefaultAsync(f => f.Key == key, cancellationToken));
        scope.Complete();
        return entity is null ? null : _factory.BuildDomain(entity);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ContentCheckPolicyFolder>> GetAllFoldersAsync(CancellationToken cancellationToken = default)
    {
        using IEFCoreScope<ContentChecksDbContext> scope = _scopeProvider.CreateScope();
        var entities = await scope.ExecuteWithContextAsync(async db => await db.PolicyFolders.ToListAsync(cancellationToken));
        scope.Complete();
        return entities.Select(_factory.BuildDomain).ToList();
    }

    /// <inheritdoc />
    public async Task<ContentCheckPolicyFolder> SaveFolderAsync(ContentCheckPolicyFolder folder, CancellationToken cancellationToken = default)
    {
        using IEFCoreScope<ContentChecksDbContext> scope = _scopeProvider.CreateScope();

        await scope.ExecuteWithContextAsync(async db =>
        {
            var existing = await db.PolicyFolders.FindAsync([folder.Key], cancellationToken);
            if (existing is null)
            {
                db.PolicyFolders.Add(_factory.BuildEntity(folder));
            }
            else
            {
                _factory.UpdateEntity(existing, folder);
            }

            await db.SaveChangesAsync(cancellationToken);
            return true;
        });

        scope.Complete();
        return folder;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteFolderAsync(Guid key, CancellationToken cancellationToken = default)
    {
        using IEFCoreScope<ContentChecksDbContext> scope = _scopeProvider.CreateScope();

        var deleted = await scope.ExecuteWithContextAsync(async db =>
        {
            var existing = await db.PolicyFolders.FindAsync([key], cancellationToken);
            if (existing is null)
            {
                return false;
            }

            db.PolicyFolders.Remove(existing);
            await db.SaveChangesAsync(cancellationToken);
            return true;
        });

        scope.Complete();
        return deleted;
    }
}
