using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Community.ContentChecks.Core.Policies;
using Umbraco.Community.ContentChecks.Web.Api.Management.Folders.Models;

namespace Umbraco.Community.ContentChecks.Web.Api.Management.Folders.Controllers;

/// <summary>
/// Controller to get a policy folder by its key.
/// </summary>
[ApiVersion("1.0")]
public class ByKeyFolderController : FoldersControllerBase
{
    private readonly IContentCheckPolicyService _policyService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ByKeyFolderController"/> class.
    /// </summary>
    public ByKeyFolderController(IContentCheckPolicyService policyService)
    {
        _policyService = policyService;
    }

    /// <summary>
    /// Get a policy folder by its key.
    /// </summary>
    /// <param name="key">The folder's key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The folder.</returns>
    [HttpGet($"{{{nameof(key)}}}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(FolderResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFolderByKey(Guid key, CancellationToken cancellationToken = default)
    {
        var folder = await _policyService.GetFolderAsync(key, cancellationToken);
        return folder is null ? FolderNotFound() : Ok(folder.ToResponseModel());
    }
}
