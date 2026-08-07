using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Community.ContentChecks.Core.Policies;
using Umbraco.Community.ContentChecks.Web.Api.Management.Folders.Models;

namespace Umbraco.Community.ContentChecks.Web.Api.Management.Tree.Controllers;

/// <summary>
/// Controller to get the ancestor folders of a tree node, for rendering a breadcrumb.
/// </summary>
[ApiVersion("1.0")]
public class AncestorsTreeController : TreeControllerBase
{
    private readonly IContentCheckPolicyService _policyService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AncestorsTreeController"/> class.
    /// </summary>
    public AncestorsTreeController(IContentCheckPolicyService policyService)
    {
        _policyService = policyService;
    }

    /// <summary>
    /// Get the ancestor folders of a folder or a policy, root-first, not including the item itself.
    /// </summary>
    /// <param name="key">The folder's or policy's key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ancestor folders, ordered root-first.</returns>
    [HttpGet($"ancestors/{{{nameof(key)}}}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(IEnumerable<FolderResponseModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAncestors(Guid key, CancellationToken cancellationToken = default)
    {
        var startingParentKey = await ResolveStartingParentKeyAsync(key, cancellationToken);
        if (startingParentKey.HasResult is false)
        {
            return NotFound(CreateProblemDetails(
                "Item not found",
                "The specified folder or policy could not be found."));
        }

        var ancestors = new List<ContentCheckPolicyFolder>();
        var currentParentKey = startingParentKey.ParentFolderKey;
        while (currentParentKey is { } parentKey)
        {
            var folder = await _policyService.GetFolderAsync(parentKey, cancellationToken);
            if (folder is null)
            {
                break;
            }

            ancestors.Add(folder);
            currentParentKey = folder.ParentFolderKey;
        }

        ancestors.Reverse();
        return Ok(ancestors.Select(f => f.ToResponseModel()));
    }

    private async Task<(bool HasResult, Guid? ParentFolderKey)> ResolveStartingParentKeyAsync(Guid key, CancellationToken cancellationToken)
    {
        var folder = await _policyService.GetFolderAsync(key, cancellationToken);
        if (folder is not null)
        {
            return (true, folder.ParentFolderKey);
        }

        var policy = await _policyService.GetPolicyAsync(key, cancellationToken);
        return policy is not null ? (true, policy.ParentFolderKey) : (false, null);
    }
}
