using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Umbraco.Community.ContentChecks.Persistence.SqlServer;

/// <summary>
/// Design-time factory used by <c>dotnet ef migrations add</c> to create a
/// <see cref="ContentChecksDbContext"/> against SQL Server without a running Umbraco site.
/// </summary>
public class ContentChecksDbContextFactory : IDesignTimeDbContextFactory<ContentChecksDbContext>
{
    /// <inheritdoc />
    public ContentChecksDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ContentChecksDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=.;Database=UmbracoCommunityContentChecks_Design;Integrated Security=true;TrustServerCertificate=true",
            x =>
            {
                x.MigrationsAssembly(typeof(ContentChecksDbContextFactory).Assembly.FullName);
                x.MigrationsHistoryTable(ContentChecksDbContext.MigrationsHistoryTableName);
            });

        return new ContentChecksDbContext(optionsBuilder.Options);
    }
}
