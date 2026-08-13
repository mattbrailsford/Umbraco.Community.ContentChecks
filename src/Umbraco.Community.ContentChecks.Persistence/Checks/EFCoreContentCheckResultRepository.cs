using Microsoft.EntityFrameworkCore;
using Umbraco.Cms.Persistence.EFCore.Scoping;
using Umbraco.Community.ContentChecks.Core.Checks;

namespace Umbraco.Community.ContentChecks.Persistence.Checks;

/// <summary>
/// EF Core implementation of <see cref="IContentCheckResultRepository"/>.
/// </summary>
internal sealed class EFCoreContentCheckResultRepository : IContentCheckResultRepository
{
    private readonly IEFCoreScopeProvider<ContentChecksDbContext> _scopeProvider;
    private readonly IContentCheckResultFactory _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="EFCoreContentCheckResultRepository"/> class.
    /// </summary>
    /// <param name="scopeProvider">The EF Core scope provider.</param>
    /// <param name="factory">Maps between domain models and entities.</param>
    public EFCoreContentCheckResultRepository(
        IEFCoreScopeProvider<ContentChecksDbContext> scopeProvider,
        IContentCheckResultFactory factory)
    {
        _scopeProvider = scopeProvider;
        _factory = factory;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ContentCheckStoredResult>> GetByContentKeyAsync(
        Guid contentKey, string? culture = null, string? segment = null, CancellationToken cancellationToken = default)
    {
        using IEFCoreScope<ContentChecksDbContext> scope = _scopeProvider.CreateScope();
        var entities = await scope.ExecuteWithContextAsync(async db => await db.Results
            .Where(r => r.ContentKey == contentKey && r.Culture == culture && r.Segment == segment)
            .ToListAsync(cancellationToken));
        scope.Complete();
        return entities.Select(_factory.BuildDomain).ToList();
    }

    /// <inheritdoc />
    public async Task ReplaceForContentAsync(
        Guid contentKey,
        string? culture,
        string? segment,
        IReadOnlyList<ContentCheckStoredResult> results,
        CancellationToken cancellationToken = default)
    {
        using IEFCoreScope<ContentChecksDbContext> scope = _scopeProvider.CreateScope();

        await scope.ExecuteWithContextAsync(async db =>
        {
            var existing = await db.Results
                .Where(r => r.ContentKey == contentKey && r.Culture == culture && r.Segment == segment)
                .ToListAsync(cancellationToken);
            db.Results.RemoveRange(existing);

            db.Results.AddRange(results.Select(_factory.BuildEntity));

            await db.SaveChangesAsync(cancellationToken);
            return true;
        });

        scope.Complete();
    }
}
