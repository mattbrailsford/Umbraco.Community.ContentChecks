using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Umbraco.Community.ContentChecks.Persistence.Sqlite;

/// <summary>
/// Design-time factory used by <c>dotnet ef migrations add</c> to create a
/// <see cref="ContentChecksDbContext"/> against SQLite without a running Umbraco site.
/// </summary>
public class ContentChecksDbContextFactory : IDesignTimeDbContextFactory<ContentChecksDbContext>
{
    /// <inheritdoc />
    public ContentChecksDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ContentChecksDbContext>();
        optionsBuilder.UseSqlite(
            "Data Source=UmbracoCommunityContentChecks_Design.sqlite",
            x =>
            {
                x.MigrationsAssembly(typeof(ContentChecksDbContextFactory).Assembly.FullName);
                x.MigrationsHistoryTable(ContentChecksDbContext.MigrationsHistoryTableName);
            });

        return new ContentChecksDbContext(optionsBuilder.Options);
    }
}
