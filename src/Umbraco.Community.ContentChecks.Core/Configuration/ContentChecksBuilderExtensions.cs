using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Community.ContentChecks.Core.Checks;
using Umbraco.Community.ContentChecks.Core.Config;

namespace Umbraco.Community.ContentChecks.Extensions;

/// <summary>
/// Extension methods for <see cref="IUmbracoBuilder"/> for Content Checks registration.
/// </summary>
public static class ContentChecksBuilderExtensions
{
    /// <summary>
    /// Gets the check collection builder.
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    /// <returns>The check collection builder.</returns>
    /// <remarks>
    /// Use this to add or exclude checks from the collection. Example:
    /// <code>
    /// builder.ContentChecks()
    ///     .Add&lt;MyCustomCheck&gt;()
    ///     .Exclude&lt;SomeUnwantedCheck&gt;();
    /// </code>
    /// </remarks>
    public static ContentCheckCollectionBuilder ContentChecks(this IUmbracoBuilder builder)
        => builder.WithCollectionBuilder<ContentCheckCollectionBuilder>();

    /// <summary>
    /// Registers Content Checks' core services: the config schema infrastructure, the result
    /// cache, the runner, and auto-discovered check types.
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    /// <returns>The builder, for chaining.</returns>
    public static IUmbracoBuilder AddContentChecks(this IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<IContentCheckConfigSchemaBuilder, ContentCheckConfigSchemaBuilder>();
        builder.Services.AddSingleton<IContentCheckConfigResolver, ContentCheckConfigResolver>();
        builder.Services.AddSingleton<IContentCheckInfrastructure, ContentCheckInfrastructure>();
        builder.Services.AddSingleton<IContentCheckResultCache, InMemoryContentCheckResultCache>();
        builder.Services.AddSingleton<IContentCheckRunner, ContentCheckRunner>();

        // Check types are auto-discovered via [ContentCheck] + IDiscoverable, same mechanism as
        // Umbraco.AI's guardrail evaluators -- not OrderedCollectionBuilder, which is for
        // middleware pipelines, a different shape of problem.
        builder.ContentChecks()
            .Add(() => builder.TypeLoader.GetTypesWithAttribute<IContentCheck, ContentCheckAttribute>(cache: true));

        return builder;
    }
}
