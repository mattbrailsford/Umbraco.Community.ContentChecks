using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Community.ContentChecks.Core.Policies;
using Umbraco.Community.ContentChecks.Web.Api.Management.Guards.Models;

namespace Umbraco.Community.ContentChecks.Web.Api.Management.Guards.Controllers;

/// <summary>
/// Controller to get a guard by its key.
/// </summary>
[ApiVersion("1.0")]
public class ByKeyGuardController : GuardsControllerBase
{
    private readonly IGuardService _guardService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="ByKeyGuardController"/> class.
    /// </summary>
    /// <param name="guardService">Guard CRUD and lookup.</param>
    /// <param name="umbracoMapper">Maps domain models to API response models.</param>
    public ByKeyGuardController(IGuardService guardService, IUmbracoMapper umbracoMapper)
    {
        _guardService = guardService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Get a guard by its key.
    /// </summary>
    /// <param name="key">The guard's key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The guard.</returns>
    [HttpGet($"{{{nameof(key)}}}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(GuardResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGuardByKey(Guid key, CancellationToken cancellationToken = default)
    {
        var guard = await _guardService.GetByKeyAsync(key, cancellationToken);
        return guard is null ? GuardNotFound() : Ok(_umbracoMapper.Map<GuardResponseModel>(guard));
    }
}
