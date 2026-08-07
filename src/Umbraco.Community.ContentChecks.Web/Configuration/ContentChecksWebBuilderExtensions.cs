using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Api.Common.DependencyInjection;
using Umbraco.Cms.Api.Common.OpenApi;
using Microsoft.AspNetCore.Authorization;
using Umbraco.Cms.Api.Management.OpenApi;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Community.ContentChecks.Core;
using Umbraco.Community.ContentChecks.Web;
using Umbraco.Community.ContentChecks.Web.Authorization;

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
                .WithBackOfficeAuthentication();
        });

        // Umbraco's own equivalent built-in policies (SectionAccessContent, etc.) build on an
        // AllowedApplicationRequirement/AllowedApplicationHandler pair that's internal to the CMS,
        // so this package registers its own requirement/handler of the same shape instead.
        builder.Services.AddSingleton<IAuthorizationHandler, ContentChecksSectionAccessHandler>();
        builder.Services.AddAuthorization(o =>
        {
            o.AddPolicy(ContentChecksAuthorizationPolicies.SectionAccessContentChecks, policy =>
                policy.Requirements.Add(new ContentChecksSectionAccessRequirement()));
        });

        return builder;
    }
}
