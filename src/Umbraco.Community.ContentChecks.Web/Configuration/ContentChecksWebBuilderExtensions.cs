using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Umbraco.Cms.Api.Common.DependencyInjection;
using Umbraco.Cms.Api.Common.OpenApi;
using Umbraco.Cms.Api.Management.OpenApi;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Web.Common.ApplicationBuilder;
using Umbraco.Community.ContentChecks.Core;
using Umbraco.Community.ContentChecks.Web;
using Umbraco.Community.ContentChecks.Web.Api.Common.Configuration;
using Umbraco.Community.ContentChecks.Web.Api.Management.ApplicableChecks.Mapping;
using Umbraco.Community.ContentChecks.Web.Api.Management.CheckTypes.Mapping;
using Umbraco.Community.ContentChecks.Web.Api.Management.Guards.Mapping;
using Umbraco.Community.ContentChecks.Web.Api.Management.Evaluation.Mapping;
using Umbraco.Community.ContentChecks.Web.Api.Management.Folders.Mapping;
using Umbraco.Community.ContentChecks.Web.Api.Management.Policies.Mapping;
using Umbraco.Community.ContentChecks.Web.Authorization;
using Umbraco.Community.ContentChecks.Web.Runs;
using Umbraco.Community.ContentChecks.Web.Triggers;

namespace Umbraco.Community.ContentChecks.Extensions;

/// <summary>
/// Extension methods for <see cref="IUmbracoBuilder"/> for Content Checks web/Management API
/// registration.
/// </summary>
public static class ContentChecksWebBuilderExtensions
{
    /// <summary>
    /// Registers the Content Checks Management API: the OpenAPI document, JSON options, and the
    /// section-access authorization policy every controller requires.
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    /// <returns>The builder, for chaining.</returns>
    public static IUmbracoBuilder AddContentChecksWeb(this IUmbracoBuilder builder)
    {
        builder.Services.AddControllers()
            .AddJsonOptions(Constants.ManagementApi.ApiName, options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

        builder.AddBackOfficeOpenApiDocument(Constants.ManagementApi.ApiName, document =>
        {
            document
                .WithTitle(Constants.ManagementApi.ApiTitle)
                .WithUiTitle(Constants.ManagementApi.ApiTitle)
                .WithJsonOptions(Constants.ManagementApi.ApiName)
                .WithBackOfficeAuthentication()
                .ConfigureOpenApiOptions(options => options.AddSchemaTransformer<ContentChecksEnumSchemaTransformer>());
        });

        // Umbraco's own equivalent built-in policies (SectionAccessContent, etc.) build on an
        // AllowedApplicationRequirement/AllowedApplicationHandler pair that's internal to the CMS,
        // so this package registers its own requirement/handler of the same shape instead.
        builder.Services.AddSingleton<IAuthorizationHandler, ContentChecksSectionAccessHandler>();
        builder.Services.AddSingleton<IAuthorizationHandler, ContentChecksSettingsSectionAccessHandler>();
        builder.Services.AddAuthorization(o =>
        {
            o.AddPolicy(ContentChecksAuthorizationPolicies.SectionAccessContentChecks, policy =>
                policy.Requirements.Add(new ContentChecksSectionAccessRequirement()));
            o.AddPolicy(ContentChecksAuthorizationPolicies.SectionAccessSettings, policy =>
                policy.Requirements.Add(new ContentChecksSettingsSectionAccessRequirement()));
        });

        // The live-run mechanism: a singleton tracker holding run state, a singleton queue
        // decoupling "a run was started" from "the worker has gotten to it", the worker itself
        // as a hosted background service, and a SignalR hub the worker pushes progress through
        // instead of a client having to poll for it.
        builder.Services.AddSingleton<IContentCheckRunTracker, ContentCheckRunTracker>();
        builder.Services.AddSingleton<IContentCheckRunQueue, ContentCheckRunQueue>();
        builder.Services.AddSingleton<IContentCheckRunNotifier, ContentCheckRunNotifier>();
        builder.Services.AddHostedService<ContentCheckRunBackgroundService>();

        // SignalR's hub protocol has its own JSON serializer, entirely separate from the named
        // MVC JsonOptions above -- without this, an enum (RequirementLevel, ContentCheckStatus,
        // etc.) on a pushed message serializes as its raw int, while the same type serializes as
        // its string name when read back over REST, since only the MVC options carry the
        // JsonStringEnumConverter.
        builder.Services.AddSignalR().AddJsonProtocol(options =>
        {
            options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });
        builder.Services.Configure<UmbracoPipelineOptions>(options =>
        {
            options.AddFilter(new UmbracoPipelineFilter(nameof(ContentCheckRunHub))
            {
                PostMapEndpoints = endpoints => endpoints.MapHub<ContentCheckRunHub>(Constants.ManagementApi.RunHubPath),
            });
        });

        // The publish gate: cancels a publish when the content's last stored report blocks the
        // current user. Needs IHttpContextAccessor to tell an interactive publish (blockable) apart
        // from a scheduled/background one (never blocked) -- see docs/triggers-spec.md.
        builder.Services.AddHttpContextAccessor();
        builder.AddNotificationAsyncHandler<ContentPublishingNotification, ContentPublishingGateHandler>();

        builder.WithCollectionBuilder<MapDefinitionCollectionBuilder>()
            .Add<PolicyMapDefinition>()
            .Add<FolderMapDefinition>()
            .Add<ApplicableChecksMapDefinition>()
            .Add<EvaluationMapDefinition>()
            .Add<CheckTypesMapDefinition>()
            .Add<GuardMapDefinition>();

        return builder;
    }
}
