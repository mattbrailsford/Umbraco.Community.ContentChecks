using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Community.ContentChecks.Core.Policies;
using Umbraco.Community.ContentChecks.Web.Api.Management.Tree.Models;

namespace Umbraco.Community.ContentChecks.Web.Api.Management.Tree.Controllers;

/// <summary>
/// Controller to get the children of a tree node — the root, or a given folder.
/// </summary>
[ApiVersion("1.0")]
public class ChildrenTreeController : TreeControllerBase
{
    private readonly IContentCheckPolicyService _policyService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChildrenTreeController"/> class.
    /// </summary>
    public ChildrenTreeController(IContentCheckPolicyService policyService)
    {
        _policyService = policyService;
    }

    /// <summary>
    /// Get the folders and policies directly inside a tree node.
    /// </summary>
    /// <param name="parentKey">The folder to list children of. Omit for the root.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Every folder and policy directly inside <paramref name="parentKey"/> (or the root).</returns>
    [HttpGet("children")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(IEnumerable<TreeItemResponseModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TreeItemResponseModel>>> GetChildren(
        Guid? parentKey = null, CancellationToken cancellationToken = default)
    {
        var allFolders = await _policyService.GetAllFoldersAsync(cancellationToken);
        var allPolicies = await _policyService.GetAllPoliciesAsync(cancellationToken);

        var childFolders = allFolders.Where(f => f.ParentFolderKey == parentKey).ToList();
        var childPolicies = allPolicies.Where(p => p.ParentFolderKey == parentKey);

        var folderItems = await Task.WhenAll(childFolders.Select(async f => new TreeItemResponseModel
        {
            Key = f.Key,
            Name = f.Name,
            IsFolder = true,
            HasChildren = await _policyService.FolderHasChildrenAsync(f.Key, cancellationToken),
            ParentFolderKey = f.ParentFolderKey,
        }));

        var policyItems = childPolicies.Select(p => new TreeItemResponseModel
        {
            Key = p.Key,
            Name = p.Name,
            IsFolder = false,
            HasChildren = false,
            ParentFolderKey = p.ParentFolderKey,
        });

        return Ok(folderItems.Concat(policyItems).OrderBy(i => i.IsFolder ? 0 : 1).ThenBy(i => i.Name));
    }
}
