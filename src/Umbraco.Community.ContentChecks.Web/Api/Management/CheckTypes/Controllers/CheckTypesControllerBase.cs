using Microsoft.AspNetCore.Mvc;
using Umbraco.Community.ContentChecks.Web.Api.Common.Controllers;
using Umbraco.Community.ContentChecks.Web.Api.Common.Routing;

namespace Umbraco.Community.ContentChecks.Web.Api.Management.CheckTypes.Controllers;

/// <summary>
/// Base controller for the registered check-types Management API endpoint.
/// </summary>
[ApiExplorerSettings(GroupName = Constants.ManagementApi.Feature.CheckTypes.GroupName)]
[ContentChecksVersionedManagementApiRoute(Constants.ManagementApi.Feature.CheckTypes.RouteSegment)]
public abstract class CheckTypesControllerBase : ContentChecksManagementControllerBase;
