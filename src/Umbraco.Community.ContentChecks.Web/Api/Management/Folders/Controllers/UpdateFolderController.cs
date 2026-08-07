using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Community.ContentChecks.Core.Policies;
using Umbraco.Community.ContentChecks.Web.Api.Management.Folders.Models;

namespace Umbraco.Community.ContentChecks.Web.Api.Management.Folders.Controllers;

/// <summary>
/// Controller to update an existing policy folder — rename, move (via
/// <see cref="FolderRequestModel.ParentFolderKey"/>), or both, in one call.
/// </summary>
[ApiVersion("1.0")]
public class UpdateFolderController : FoldersControllerBase
{
    private readonly IContentCheckPolicyService _policyService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateFolderController"/> class.
    /// </summary>
    /// <param name="policyService">Policy/folder CRUD and lookup.</param>
    /// <param name="umbracoMapper">Maps API request/response models to domain models.</param>
    public UpdateFolderController(IContentCheckPolicyService policyService, IUmbracoMapper umbracoMapper)
    {
        _policyService = policyService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Update a policy folder.
    /// </summary>
    /// <param name="key">The folder's key.</param>
    /// <param name="requestModel">The folder's new state.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated folder.</returns>
    [HttpPut($"{{{nameof(key)}}}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(FolderResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateFolder(Guid key, FolderRequestModel requestModel, CancellationToken cancellationToken = default)
    {
        var existing = await _policyService.GetFolderAsync(key, cancellationToken);
        if (existing is null)
        {
            return FolderNotFound();
        }

        var folder = _umbracoMapper.Map<FolderRequestModel, ContentCheckPolicyFolder>(requestModel)!;
        folder.Key = key;

        var updated = await _policyService.SaveFolderAsync(folder, cancellationToken);
        return Ok(_umbracoMapper.Map<FolderResponseModel>(updated));
    }
}
