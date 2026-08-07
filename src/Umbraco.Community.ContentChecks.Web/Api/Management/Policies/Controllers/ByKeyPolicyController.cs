using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Mapping;
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
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="ByKeyPolicyController"/> class.
    /// </summary>
    /// <param name="policyService">Policy CRUD and lookup.</param>
    /// <param name="umbracoMapper">Maps domain models to API response models.</param>
    public ByKeyPolicyController(IContentCheckPolicyService policyService, IUmbracoMapper umbracoMapper)
    {
        _policyService = policyService;
        _umbracoMapper = umbracoMapper;
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
        return policy is null ? PolicyNotFound() : Ok(_umbracoMapper.Map<PolicyResponseModel>(policy));
    }
}
