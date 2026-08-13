using Microsoft.EntityFrameworkCore;
using Umbraco.Cms.Persistence.EFCore.Scoping;
using Umbraco.Community.ContentChecks.Core.Policies;

namespace Umbraco.Community.ContentChecks.Persistence.Policies;

/// <summary>
/// EF Core implementation of <see cref="IGuardRepository"/>.
/// </summary>
internal sealed class EFCoreGuardRepository : IGuardRepository
{
    private readonly IEFCoreScopeProvider<ContentChecksDbContext> _scopeProvider;
    private readonly IGuardFactory _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="EFCoreGuardRepository"/> class.
    /// </summary>
    /// <param name="scopeProvider">The EF Core scope provider.</param>
    /// <param name="factory">Maps between domain models and entities.</param>
    public EFCoreGuardRepository(
        IEFCoreScopeProvider<ContentChecksDbContext> scopeProvider,
        IGuardFactory factory)
    {
        _scopeProvider = scopeProvider;
        _factory = factory;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Guard>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using IEFCoreScope<ContentChecksDbContext> scope = _scopeProvider.CreateScope();
        var entities = await scope.ExecuteWithContextAsync(async db => await db.Guards.ToListAsync(cancellationToken));
        scope.Complete();
        return entities.Select(_factory.BuildDomain).ToList();
    }

    /// <inheritdoc />
    public async Task<Guard?> GetByKeyAsync(Guid key, CancellationToken cancellationToken = default)
    {
        using IEFCoreScope<ContentChecksDbContext> scope = _scopeProvider.CreateScope();
        var entity = await scope.ExecuteWithContextAsync(async db =>
            await db.Guards.FirstOrDefaultAsync(s => s.Key == key, cancellationToken));
        scope.Complete();
        return entity is null ? null : _factory.BuildDomain(entity);
    }

    /// <inheritdoc />
    public async Task<Guard> SaveAsync(Guard guard, CancellationToken cancellationToken = default)
    {
        using IEFCoreScope<ContentChecksDbContext> scope = _scopeProvider.CreateScope();

        await scope.ExecuteWithContextAsync(async db =>
        {
            var existing = await db.Guards.FindAsync([guard.Key], cancellationToken);
            if (existing is null)
            {
                db.Guards.Add(_factory.BuildEntity(guard));
            }
            else
            {
                _factory.UpdateEntity(existing, guard);
            }

            await db.SaveChangesAsync(cancellationToken);
            return true;
        });

        scope.Complete();
        return guard;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid key, CancellationToken cancellationToken = default)
    {
        using IEFCoreScope<ContentChecksDbContext> scope = _scopeProvider.CreateScope();

        var deleted = await scope.ExecuteWithContextAsync(async db =>
        {
            var existing = await db.Guards.FindAsync([key], cancellationToken);
            if (existing is null)
            {
                return false;
            }

            db.Guards.Remove(existing);
            await db.SaveChangesAsync(cancellationToken);
            return true;
        });

        scope.Complete();
        return deleted;
    }
}
