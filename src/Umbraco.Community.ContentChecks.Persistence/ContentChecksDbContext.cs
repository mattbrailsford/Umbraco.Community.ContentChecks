using Microsoft.EntityFrameworkCore;
using Umbraco.Cms.Core;
using Umbraco.Community.ContentChecks.Persistence.Policies;

namespace Umbraco.Community.ContentChecks.Persistence;

/// <summary>
/// EF Core DbContext for Content Checks persistence.
/// </summary>
public class ContentChecksDbContext : DbContext
{
    /// <summary>
    /// The shared migrations history table name for Content Checks.
    /// </summary>
    internal const string MigrationsHistoryTableName = "__EFMigrationsHistory_UmbracoCommunityContentChecks";

    /// <summary>
    /// Check policies.
    /// </summary>
    internal DbSet<ContentCheckPolicyEntity> Policies { get; set; } = null!;

    /// <summary>
    /// Check policy folders.
    /// </summary>
    internal DbSet<ContentCheckPolicyFolderEntity> PolicyFolders { get; set; } = null!;

    /// <summary>
    /// Guards.
    /// </summary>
    internal DbSet<GuardEntity> Guards { get; set; } = null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentChecksDbContext"/> class.
    /// </summary>
    /// <param name="options">The context options.</param>
    public ContentChecksDbContext(DbContextOptions<ContentChecksDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Configures the EF Core database provider with the correct migrations assembly.
    /// </summary>
    /// <param name="options">The options builder to configure.</param>
    /// <param name="connectionString">The connection string.</param>
    /// <param name="providerName">The database provider name.</param>
    internal static void ConfigureProvider(DbContextOptionsBuilder options, string? connectionString, string? providerName)
    {
        if (string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(providerName))
        {
            return;
        }

        switch (providerName)
        {
            case Constants.ProviderNames.SQLServer:
                options.UseSqlServer(connectionString, x =>
                {
                    x.MigrationsAssembly("Umbraco.Community.ContentChecks.Persistence.SqlServer");
                    x.MigrationsHistoryTable(MigrationsHistoryTableName);
                });
                break;

            case Constants.ProviderNames.SQLLite:
            case "Microsoft.Data.SQLite":
                options.UseSqlite(connectionString, x =>
                {
                    x.MigrationsAssembly("Umbraco.Community.ContentChecks.Persistence.Sqlite");
                    x.MigrationsHistoryTable(MigrationsHistoryTableName);
                });
                break;

            default:
                throw new InvalidOperationException(
                    $"The database provider '{providerName}' is not supported by Umbraco.Community.ContentChecks.Persistence. " +
                    "Supported providers: SQL Server, SQLite.");
        }
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ContentCheckPolicyEntity>(entity =>
        {
            entity.ToTable("umbracoCommunityContentChecksPolicy");
            entity.HasKey(e => e.Key);

            entity.Property(e => e.Alias)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.ContentTypeKey)
                .IsRequired();

            entity.Property(e => e.RulesJson)
                .IsRequired();

            entity.HasIndex(e => e.Alias).IsUnique();
            entity.HasIndex(e => e.ContentTypeKey);
            entity.HasIndex(e => e.ParentFolderKey);
        });

        modelBuilder.Entity<ContentCheckPolicyFolderEntity>(entity =>
        {
            entity.ToTable("umbracoCommunityContentChecksPolicyFolder");
            entity.HasKey(e => e.Key);

            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .IsRequired();

            entity.HasIndex(e => e.ParentFolderKey);
        });

        modelBuilder.Entity<GuardEntity>(entity =>
        {
            entity.ToTable("umbracoCommunityContentChecksGuard");
            entity.HasKey(e => e.Key);

            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.RulesJson)
                .IsRequired();
        });
    }
}
