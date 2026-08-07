using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Community.ContentChecks.Core.Policies;
using Umbraco.Community.ContentChecks.Web.Api.Management.Policies.Models;

namespace Umbraco.Community.ContentChecks.Web.Api.Management.Policies.Controllers;

/// <summary>
/// Controller to get a check policy by its key.
/// </summary>
[ApiVersion("1.0")]
public class ByKeyPolicyController : PoliciesControllerBase
{
    private readonly IContentCheckPolicyService _policyService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ByKeyPolicyController"/> class.
    /// </summary>
    public ByKeyPolicyController(IContentCheckPolicyService policyService)
    {
        _policyService = policyService;
    }

    /// <summary>
    /// Get a check policy by its key.
    /// </summary>
    /// <param name="key">The policy's key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The policy.</returns>
    [HttpGet($"{{{nameof(key)}}}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(PolicyResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPolicyByKey(Guid key, CancellationToken cancellationToken = default)
    {
        var policy = await _policyService.GetPolicyAsync(key, cancellationToken);
        return policy is null ? PolicyNotFound() : Ok(policy.ToResponseModel());
    }
}
