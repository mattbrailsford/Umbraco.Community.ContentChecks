using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Community.ContentChecks.Core.Policies;
using Umbraco.Community.ContentChecks.Web.Api.Management.Policies.Models;

namespace Umbraco.Community.ContentChecks.Web.Api.Management.Policies.Controllers;

/// <summary>
/// Controller to update an existing check policy.
/// </summary>
[ApiVersion("1.0")]
public class UpdatePolicyController : PoliciesControllerBase
{
    private readonly IContentCheckPolicyService _policyService;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdatePolicyController"/> class.
    /// </summary>
    public UpdatePolicyController(IContentCheckPolicyService policyService)
    {
        _policyService = policyService;
    }

    /// <summary>
    /// Update a check policy — including moving it into or out of a folder via
    /// <see cref="PolicyRequestModel.ParentFolderKey"/>. Not a separate move endpoint.
    /// </summary>
    /// <param name="key">The policy's key.</param>
    /// <param name="requestModel">The policy's new state.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated policy.</returns>
    [HttpPut($"{{{nameof(key)}}}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(PolicyResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePolicy(Guid key, PolicyRequestModel requestModel, CancellationToken cancellationToken = default)
    {
        var existing = await _policyService.GetPolicyAsync(key, cancellationToken);
        if (existing is null)
        {
            return PolicyNotFound();
        }

        var policy = requestModel.ToDomain(key);

        try
        {
            var updated = await _policyService.SavePolicyAsync(policy, cancellationToken);
            return Ok(updated.ToResponseModel());
        }
        catch (InvalidOperationException ex)
        {
            return AliasInUse(ex.Message);
        }
    }
}
