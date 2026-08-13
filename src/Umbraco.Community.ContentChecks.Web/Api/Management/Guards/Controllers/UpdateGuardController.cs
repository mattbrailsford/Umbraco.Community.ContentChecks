using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Community.ContentChecks.Core.Policies;
using Umbraco.Community.ContentChecks.Web.Api.Management.Guards.Models;

namespace Umbraco.Community.ContentChecks.Web.Api.Management.Guards.Controllers;

/// <summary>
/// Controller to update an existing guard.
/// </summary>
[ApiVersion("1.0")]
public class UpdateGuardController : GuardsControllerBase
{
    private readonly IGuardService _guardService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateGuardController"/> class.
    /// </summary>
    /// <param name="guardService">Guard CRUD and lookup.</param>
    /// <param name="umbracoMapper">Maps API request/response models to domain models.</param>
    public UpdateGuardController(IGuardService guardService, IUmbracoMapper umbracoMapper)
    {
        _guardService = guardService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Update a guard.
    /// </summary>
    /// <param name="key">The guard's key.</param>
    /// <param name="requestModel">The guard's new state.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated guard.</returns>
    [HttpPut($"{{{nameof(key)}}}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(GuardResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateGuard(Guid key, GuardRequestModel requestModel, CancellationToken cancellationToken = default)
    {
        var existing = await _guardService.GetByKeyAsync(key, cancellationToken);
        if (existing is null)
        {
            return GuardNotFound();
        }

        var guard = _umbracoMapper.Map<GuardRequestModel, Guard>(requestModel)!;
        guard.Key = key;

        var updated = await _guardService.SaveAsync(guard, cancellationToken);
        return Ok(_umbracoMapper.Map<GuardResponseModel>(updated));
    }
}
