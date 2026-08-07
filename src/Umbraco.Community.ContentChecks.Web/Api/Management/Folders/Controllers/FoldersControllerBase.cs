using Microsoft.AspNetCore.Mvc;
using Umbraco.Community.ContentChecks.Web.Api.Common.Controllers;
using Umbraco.Community.ContentChecks.Web.Api.Common.Routing;

namespace Umbraco.Community.ContentChecks.Web.Api.Management.Folders.Controllers;

/// <summary>
/// Base controller for policy folder Management API endpoints.
/// </summary>
[ApiExplorerSettings(GroupName = Constants.ManagementApi.Feature.Folders.GroupName)]
[ContentChecksVersionedManagementApiRoute(Constants.ManagementApi.Feature.Folders.RouteSegment)]
public abstract class FoldersControllerBase : ContentChecksManagementControllerBase
{
    /// <summary>
    /// Returns a 404 Not Found response for a folder.
    /// </summary>
    /// <returns>A 404 Not Found result.</returns>
    protected IActionResult FolderNotFound() => NotFound(CreateProblemDetails(
        "Folder not found",
        "The specified folder could not be found."));

    /// <summary>
    /// Returns a 400 Bad Request response for a folder that still has policies or subfolders in it.
    /// </summary>
    /// <returns>A 400 Bad Request result.</returns>
    protected IActionResult FolderNotEmpty() => BadRequest(CreateProblemDetails(
        "Folder not empty",
        "The folder still contains policies or subfolders. Empty it first."));
}
