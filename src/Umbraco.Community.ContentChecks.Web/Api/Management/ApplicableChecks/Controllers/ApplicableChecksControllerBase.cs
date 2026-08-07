using Microsoft.AspNetCore.Mvc;
using Umbraco.Community.ContentChecks.Web.Api.Common.Controllers;
using Umbraco.Community.ContentChecks.Web.Api.Common.Routing;

namespace Umbraco.Community.ContentChecks.Web.Api.Management.ApplicableChecks.Controllers;

/// <summary>
/// Base controller for the Applicable Checks Management API endpoint.
/// </summary>
[ApiExplorerSettings(GroupName = Constants.ManagementApi.Feature.ApplicableChecks.GroupName)]
[ContentChecksVersionedManagementApiRoute(Constants.ManagementApi.Feature.ApplicableChecks.RouteSegment)]
public abstract class ApplicableChecksControllerBase : ContentChecksManagementControllerBase
{
    /// <summary>
    /// Returns a 404 Not Found response for a content item.
    /// </summary>
    /// <returns>A 404 Not Found result.</returns>
    protected IActionResult ContentNotFound() => NotFound(CreateProblemDetails(
        "Content not found",
        "The specified content item could not be found."));
}
