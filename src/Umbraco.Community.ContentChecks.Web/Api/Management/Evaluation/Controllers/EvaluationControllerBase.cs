using Microsoft.AspNetCore.Mvc;
using Umbraco.Community.ContentChecks.Web.Api.Common.Controllers;
using Umbraco.Community.ContentChecks.Web.Api.Common.Routing;

namespace Umbraco.Community.ContentChecks.Web.Api.Management.Evaluation.Controllers;

/// <summary>
/// Base controller for the evaluation (live-run) Management API endpoints.
/// </summary>
[ApiExplorerSettings(GroupName = Constants.ManagementApi.Feature.Evaluation.GroupName)]
[ContentChecksVersionedManagementApiRoute(Constants.ManagementApi.Feature.Evaluation.RouteSegment)]
public abstract class EvaluationControllerBase : ContentChecksManagementControllerBase
{
    /// <summary>
    /// Returns a 404 Not Found response for a run.
    /// </summary>
    /// <returns>A 404 Not Found result.</returns>
    protected IActionResult RunNotFound() => NotFound(CreateProblemDetails(
        "Run not found",
        "The specified run could not be found -- it may have finished a long time ago, or the app has restarted since it started."));
}
