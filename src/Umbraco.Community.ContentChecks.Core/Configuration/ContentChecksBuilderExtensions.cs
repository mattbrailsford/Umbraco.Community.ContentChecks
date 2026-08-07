using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Community.ContentChecks.Core.Checks;
using Umbraco.Community.ContentChecks.Core.Config;
using Umbraco.Community.ContentChecks.Core.Policies;
using Umbraco.Community.ContentChecks.Core.Policies.Elements;

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
    /// Gets the element source collection builder.
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    /// <returns>The element source collection builder.</returns>
    public static ContentCheckElementSourceCollectionBuilder ContentCheckElementSources(this IUmbracoBuilder builder)
        => builder.WithCollectionBuilder<ContentCheckElementSourceCollectionBuilder>();

    /// <summary>
    /// Registers Content Checks' core services: the config schema infrastructure, the result
    /// cache, the runner, auto-discovered check types, policy CRUD, element sources, and the
    /// resolution/effective-checks services.
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

        // Check types are auto-discovered via [ContentCheck] + IDiscoverable -- not
        // OrderedCollectionBuilder, which is for middleware pipelines, a different shape of problem.
        builder.ContentChecks()
            .Add(() => builder.TypeLoader.GetTypesWithAttribute<IContentCheck, ContentCheckAttribute>(cache: true));

        // Element sources are auto-discovered the same way, keyed by property-editor alias rather
        // than a Cost/Kind pair.
        builder.ContentCheckElementSources()
            .Add(() => builder.TypeLoader.GetTypesWithAttribute<IContentCheckElementSource, ContentCheckElementSourceAttribute>(cache: true));

        builder.Services.AddSingleton<IContentCheckPolicyRepository, InMemoryContentCheckPolicyRepository>();
        builder.Services.AddSingleton<IContentCheckPolicyService, ContentCheckPolicyService>();

        builder.Services.AddSingleton<IContentCheckResolutionService, ContentCheckResolutionService>();
        builder.Services.AddSingleton<IContentCheckEffectiveChecksService, ContentCheckEffectiveChecksService>();
        builder.Services.AddSingleton<IContentCheckEvaluationService, ContentCheckEvaluationService>();

        return builder;
    }
}
