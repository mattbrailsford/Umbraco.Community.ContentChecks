using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Community.ContentChecks.Core.Policies;
using Umbraco.Community.ContentChecks.Persistence;
using Umbraco.Community.ContentChecks.Persistence.Notifications;
using Umbraco.Community.ContentChecks.Persistence.Policies;
using Umbraco.Extensions;

namespace Umbraco.Community.ContentChecks.Extensions;

/// <summary>
/// Extension methods for <see cref="IUmbracoBuilder"/> for Content Checks persistence registration.
/// </summary>
public static class ContentChecksPersistenceBuilderExtensions
{
    /// <summary>
    /// Registers EF Core persistence for Content Checks — the database context, and the EF Core
    /// repository, overriding the in-memory default. Shares the host site's own Umbraco connection;
    /// this package is small enough that it doesn't need its own separate connection-string override.
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    /// <returns>The builder, for chaining.</returns>
    public static IUmbracoBuilder AddContentChecksPersistence(this IUmbracoBuilder builder)
    {
        builder.Services.AddUmbracoDbContext<ContentChecksDbContext>(
            (options, connectionString, providerName, _) =>
                ContentChecksDbContext.ConfigureProvider(options, connectionString, providerName),
            shareUmbracoConnection: true);

        builder.Services.AddSingleton<IContentCheckPolicyFactory, ContentCheckPolicyFactory>();
        builder.Services.AddSingleton<IContentCheckPolicyRepository, EFCoreContentCheckPolicyRepository>();

        builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, RunContentChecksMigrationNotificationHandler>();

        return builder;
    }
}
