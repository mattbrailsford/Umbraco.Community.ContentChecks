using Umbraco.Cms.Web.Common.Routing;

namespace Umbraco.Community.ContentChecks.Web.Api.Common.Routing;

/// <summary>
/// Attribute for defining versioned Content Checks Management API routes.
/// </summary>
/// <param name="template">The route template.</param>
public class ContentChecksVersionedManagementApiRouteAttribute(string template)
    : BackOfficeRouteAttribute($"{Constants.ManagementApi.BackofficePath}/v{{version:apiVersion}}/{template.TrimStart('/')}");
