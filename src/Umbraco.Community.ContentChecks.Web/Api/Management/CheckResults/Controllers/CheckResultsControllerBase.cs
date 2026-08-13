using Microsoft.AspNetCore.Mvc;
using Umbraco.Community.ContentChecks.Web.Api.Common.Controllers;
using Umbraco.Community.ContentChecks.Web.Api.Common.Routing;

namespace Umbraco.Community.ContentChecks.Web.Api.Management.CheckResults.Controllers;

/// <summary>
/// Base controller for the check results Management API endpoints.
/// </summary>
[ApiExplorerSettings(GroupName = Constants.ManagementApi.Feature.CheckResults.GroupName)]
[ContentChecksVersionedManagementApiRoute(Constants.ManagementApi.Feature.CheckResults.RouteSegment)]
public abstract class CheckResultsControllerBase : ContentChecksManagementControllerBase
{
}
