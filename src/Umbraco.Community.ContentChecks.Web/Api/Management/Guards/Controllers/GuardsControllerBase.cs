using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Community.ContentChecks.Web.Api.Common.Controllers;
using Umbraco.Community.ContentChecks.Web.Api.Common.Routing;
using Umbraco.Community.ContentChecks.Web.Authorization;

namespace Umbraco.Community.ContentChecks.Web.Api.Management.Guards.Controllers;

/// <summary>
/// Base controller for guard Management API endpoints. Additionally requires
/// Umbraco's own built-in Settings section, on top of Content Checks' own section access already
/// required by <see cref="ContentChecksManagementControllerBase"/> — guards are a
/// site-configuration concern, per docs/guard-spec.md.
/// </summary>
[ApiExplorerSettings(GroupName = Constants.ManagementApi.Feature.Guards.GroupName)]
[ContentChecksVersionedManagementApiRoute(Constants.ManagementApi.Feature.Guards.RouteSegment)]
[Authorize(Policy = ContentChecksAuthorizationPolicies.SectionAccessSettings)]
public abstract class GuardsControllerBase : ContentChecksManagementControllerBase
{
    /// <summary>
    /// Returns a 404 Not Found response for a guard.
    /// </summary>
    /// <returns>A 404 Not Found result.</returns>
    protected IActionResult GuardNotFound() => NotFound(CreateProblemDetails(
        "Guard not found",
        "The specified guard could not be found."));
}
