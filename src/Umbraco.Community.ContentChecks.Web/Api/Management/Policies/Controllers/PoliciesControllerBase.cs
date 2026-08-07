using Microsoft.AspNetCore.Mvc;
using Umbraco.Community.ContentChecks.Web.Api.Common.Controllers;
using Umbraco.Community.ContentChecks.Web.Api.Common.Routing;

namespace Umbraco.Community.ContentChecks.Web.Api.Management.Policies.Controllers;

/// <summary>
/// Base controller for check policy Management API endpoints.
/// </summary>
[ApiExplorerSettings(GroupName = Constants.ManagementApi.Feature.Policies.GroupName)]
[ContentChecksVersionedManagementApiRoute(Constants.ManagementApi.Feature.Policies.RouteSegment)]
public abstract class PoliciesControllerBase : ContentChecksManagementControllerBase
{
    /// <summary>
    /// Returns a 404 Not Found response for a policy.
    /// </summary>
    protected IActionResult PolicyNotFound() => NotFound(CreateProblemDetails(
        "Policy not found",
        "The specified check policy could not be found."));

    /// <summary>
    /// Returns a 400 Bad Request response for an alias already in use by another policy.
    /// </summary>
    protected IActionResult AliasInUse(string detail) => BadRequest(CreateProblemDetails("Alias already in use", detail));
}
