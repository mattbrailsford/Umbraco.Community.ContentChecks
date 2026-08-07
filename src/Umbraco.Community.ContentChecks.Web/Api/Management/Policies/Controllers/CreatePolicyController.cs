using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Community.ContentChecks.Core.Policies;
using Umbraco.Community.ContentChecks.Web.Api.Management.Policies.Models;

namespace Umbraco.Community.ContentChecks.Web.Api.Management.Policies.Controllers;

/// <summary>
/// Controller to create a new check policy.
/// </summary>
[ApiVersion("1.0")]
public class CreatePolicyController : PoliciesControllerBase
{
    private readonly IContentCheckPolicyService _policyService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreatePolicyController"/> class.
    /// </summary>
    public CreatePolicyController(IContentCheckPolicyService policyService)
    {
        _policyService = policyService;
    }

    /// <summary>
    /// Create a new check policy.
    /// </summary>
    /// <param name="requestModel">The policy to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created policy.</returns>
    [HttpPost]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(PolicyResponseModel), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePolicy(PolicyRequestModel requestModel, CancellationToken cancellationToken = default)
    {
        var policy = requestModel.ToDomain(requestModel.Key);

        try
        {
            var created = await _policyService.SavePolicyAsync(policy, cancellationToken);
            return CreatedAtAction(
                nameof(ByKeyPolicyController.GetPolicyByKey),
                nameof(ByKeyPolicyController).Replace("Controller", string.Empty),
                new { key = created.Key },
                created.ToResponseModel());
        }
        catch (InvalidOperationException ex)
        {
            return AliasInUse(ex.Message);
        }
    }
}
