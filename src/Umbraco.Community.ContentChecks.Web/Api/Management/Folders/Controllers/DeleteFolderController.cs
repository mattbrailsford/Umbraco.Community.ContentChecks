using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Community.ContentChecks.Core.Policies;

namespace Umbraco.Community.ContentChecks.Web.Api.Management.Folders.Controllers;

/// <summary>
/// Controller to delete a policy folder. Blocked, not cascading, if the folder still has
/// policies or subfolders in it.
/// </summary>
[ApiVersion("1.0")]
public class DeleteFolderController : FoldersControllerBase
{
    private readonly IContentCheckPolicyService _policyService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteFolderController"/> class.
    /// </summary>
    public DeleteFolderController(IContentCheckPolicyService policyService)
    {
        _policyService = policyService;
    }

    /// <summary>
    /// Delete a policy folder.
    /// </summary>
    /// <param name="key">The folder's key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete($"{{{nameof(key)}}}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteFolder(Guid key, CancellationToken cancellationToken = default)
    {
        try
        {
            var deleted = await _policyService.DeleteFolderAsync(key, cancellationToken);
            return deleted ? Ok() : FolderNotFound();
        }
        catch (InvalidOperationException)
        {
            return FolderNotEmpty();
        }
    }
}
