using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Community.ContentChecks.Core.Policies;
using Umbraco.Community.ContentChecks.Web.Api.Management.Guards.Models;

namespace Umbraco.Community.ContentChecks.Web.Api.Management.Guards.Controllers;

/// <summary>
/// Controller to get every guard.
/// </summary>
[ApiVersion("1.0")]
public class AllGuardsController : GuardsControllerBase
{
    private readonly IGuardService _guardService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="AllGuardsController"/> class.
    /// </summary>
    /// <param name="guardService">Guard CRUD and lookup.</param>
    /// <param name="umbracoMapper">Maps domain models to API response models.</param>
    public AllGuardsController(IGuardService guardService, IUmbracoMapper umbracoMapper)
    {
        _guardService = guardService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Get every guard.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Every guard.</returns>
    [HttpGet]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(IEnumerable<GuardResponseModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<GuardResponseModel>>> GetAllGuards(CancellationToken cancellationToken = default)
    {
        var guards = await _guardService.GetAllAsync(cancellationToken);
        return Ok(_umbracoMapper.MapEnumerable<Guard, GuardResponseModel>(guards));
    }
}
