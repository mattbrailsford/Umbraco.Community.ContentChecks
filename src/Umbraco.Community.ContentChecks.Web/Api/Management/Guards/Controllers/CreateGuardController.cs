using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Community.ContentChecks.Core.Policies;
using Umbraco.Community.ContentChecks.Web.Api.Management.Guards.Models;

namespace Umbraco.Community.ContentChecks.Web.Api.Management.Guards.Controllers;

/// <summary>
/// Controller to create a new guard.
/// </summary>
[ApiVersion("1.0")]
public class CreateGuardController : GuardsControllerBase
{
    private readonly IGuardService _guardService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateGuardController"/> class.
    /// </summary>
    /// <param name="guardService">Guard CRUD and lookup.</param>
    /// <param name="umbracoMapper">Maps API request/response models to domain models.</param>
    public CreateGuardController(IGuardService guardService, IUmbracoMapper umbracoMapper)
    {
        _guardService = guardService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Create a new guard.
    /// </summary>
    /// <param name="requestModel">The guard to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created guard.</returns>
    [HttpPost]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(GuardResponseModel), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateGuard(GuardRequestModel requestModel, CancellationToken cancellationToken = default)
    {
        var guard = _umbracoMapper.Map<GuardRequestModel, Guard>(requestModel)!;
        guard.Key = requestModel.Key;

        var created = await _guardService.SaveAsync(guard, cancellationToken);
        return CreatedAtAction(
            nameof(ByKeyGuardController.GetGuardByKey),
            nameof(ByKeyGuardController).Replace("Controller", string.Empty),
            new { key = created.Key },
            _umbracoMapper.Map<GuardResponseModel>(created));
    }
}
