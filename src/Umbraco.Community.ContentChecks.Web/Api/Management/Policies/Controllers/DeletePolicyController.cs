using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Community.ContentChecks.Core.Policies;

namespace Umbraco.Community.ContentChecks.Web.Api.Management.Policies.Controllers;

/// <summary>
/// Controller to delete a check policy.
/// </summary>
[ApiVersion("1.0")]
public class DeletePolicyController : PoliciesControllerBase
{
    private readonly IContentCheckPolicyService _policyService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeletePolicyController"/> class.
    /// </summary>
    /// <param name="policyService">Policy CRUD and lookup.</param>
    public DeletePolicyController(IContentCheckPolicyService policyService)
    {
        _policyService = policyService;
    }

    /// <summary>
    /// Delete a check policy.
    /// </summary>
    /// <param name="key">The policy's key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete($"{{{nameof(key)}}}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePolicy(Guid key, CancellationToken cancellationToken = default)
    {
        var deleted = await _policyService.DeletePolicyAsync(key, cancellationToken);
        return deleted ? Ok() : PolicyNotFound();
    }
}
