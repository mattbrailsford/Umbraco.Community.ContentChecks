using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Extensions;

namespace Umbraco.Community.ContentChecks.Persistence.Notifications;

/// <summary>
/// Runs pending EF Core migrations on application startup. Builds a standalone
/// <see cref="ContentChecksDbContext"/> rather than resolving one from the pooled EF Core context
/// factory — Umbraco's own scope infrastructure can share a connection onto pooled contexts in a
/// way that trips <c>SqliteDatabaseCreator.Exists()</c> during a migration check
/// (umbraco/Umbraco-CMS#22124).
/// </summary>
public sealed class RunContentChecksMigrationNotificationHandler : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RunContentChecksMigrationNotificationHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RunContentChecksMigrationNotificationHandler"/> class.
    /// </summary>
    /// <param name="configuration">Used to resolve the host's connection string/provider.</param>
    /// <param name="logger">Logger.</param>
    public RunContentChecksMigrationNotificationHandler(IConfiguration configuration, ILogger<RunContentChecksMigrationNotificationHandler> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            var connectionString = _configuration.GetUmbracoConnectionString(out var providerName);
            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogDebug("No database connection string available -- skipping Content Checks migrations (Umbraco may still be installing).");
                return;
            }

            var optionsBuilder = new DbContextOptionsBuilder<ContentChecksDbContext>();
            ContentChecksDbContext.ConfigureProvider(optionsBuilder, connectionString, providerName);

            // Downgrade PendingModelChangesWarning from exception to log so migrations can still be
            // applied during development when the model has unreleased changes.
            optionsBuilder.ConfigureWarnings(w => w.Log(RelationalEventId.PendingModelChangesWarning));

            await using var dbContext = new ContentChecksDbContext(optionsBuilder.Options);

            var pending = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
            if (pending.Any())
            {
                _logger.LogInformation("Running Umbraco Content Checks database migrations...");
                await dbContext.Database.MigrateAsync(cancellationToken);
                _logger.LogInformation("Umbraco Content Checks database migrations completed successfully.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run Umbraco Content Checks database migrations.");
            throw;
        }
    }
}
