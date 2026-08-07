using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Community.ContentChecks.Core.Policies;
using Umbraco.Community.ContentChecks.Web.Api.Management.Policies.Models;

namespace Umbraco.Community.ContentChecks.Web.Api.Management.Policies.Controllers;

/// <summary>
/// Controller to get every check policy.
/// </summary>
[ApiVersion("1.0")]
public class AllPoliciesController : PoliciesControllerBase
{
    private readonly IContentCheckPolicyService _policyService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AllPoliciesController"/> class.
    /// </summary>
    public AllPoliciesController(IContentCheckPolicyService policyService)
    {
        _policyService = policyService;
    }

    /// <summary>
    /// Get every check policy.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Every check policy.</returns>
    [HttpGet]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(IEnumerable<PolicyResponseModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<PolicyResponseModel>>> GetAllPolicies(CancellationToken cancellationToken = default)
    {
        var policies = await _policyService.GetAllPoliciesAsync(cancellationToken);
        return Ok(policies.Select(p => p.ToResponseModel()));
    }
}
