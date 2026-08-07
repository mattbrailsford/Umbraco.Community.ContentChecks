using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Community.ContentChecks.Core.Policies;
using Umbraco.Community.ContentChecks.Web.Api.Management.Folders.Models;

namespace Umbraco.Community.ContentChecks.Web.Api.Management.Folders.Controllers;

/// <summary>
/// Controller to create a new policy folder.
/// </summary>
[ApiVersion("1.0")]
public class CreateFolderController : FoldersControllerBase
{
    private readonly IContentCheckPolicyService _policyService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateFolderController"/> class.
    /// </summary>
    public CreateFolderController(IContentCheckPolicyService policyService)
    {
        _policyService = policyService;
    }

    /// <summary>
    /// Create a new policy folder.
    /// </summary>
    /// <param name="requestModel">The folder to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created folder.</returns>
    [HttpPost]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(FolderResponseModel), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateFolder(FolderRequestModel requestModel, CancellationToken cancellationToken = default)
    {
        var folder = requestModel.ToDomain(requestModel.Key);
        var created = await _policyService.SaveFolderAsync(folder, cancellationToken);

        return CreatedAtAction(
            nameof(ByKeyFolderController.GetFolderByKey),
            nameof(ByKeyFolderController).Replace("Controller", string.Empty),
            new { key = created.Key },
            created.ToResponseModel());
    }
}
