using Microsoft.AspNetCore.Mvc;
using Umbraco.Community.ContentChecks.Web.Api.Common.Controllers;
using Umbraco.Community.ContentChecks.Web.Api.Common.Routing;

namespace Umbraco.Community.ContentChecks.Web.Api.Management.Tree.Controllers;

/// <summary>
/// Base controller for the combined policy/folder tree's Management API endpoints.
/// </summary>
[ApiExplorerSettings(GroupName = Constants.ManagementApi.Feature.Tree.GroupName)]
[ContentChecksVersionedManagementApiRoute(Constants.ManagementApi.Feature.Tree.RouteSegment)]
public abstract class TreeControllerBase : ContentChecksManagementControllerBase;
