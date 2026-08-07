using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Mapping;
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
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="ByKeyFolderController"/> class.
    /// </summary>
    /// <param name="policyService">Policy/folder CRUD and lookup.</param>
    /// <param name="umbracoMapper">Maps domain models to API response models.</param>
    public ByKeyFolderController(IContentCheckPolicyService policyService, IUmbracoMapper umbracoMapper)
    {
        _policyService = policyService;
        _umbracoMapper = umbracoMapper;
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
        return folder is null ? FolderNotFound() : Ok(_umbracoMapper.Map<FolderResponseModel>(folder));
    }
}
