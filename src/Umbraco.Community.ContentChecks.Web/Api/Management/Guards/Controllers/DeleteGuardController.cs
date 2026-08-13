using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Community.ContentChecks.Core.Policies;

namespace Umbraco.Community.ContentChecks.Web.Api.Management.Guards.Controllers;

/// <summary>
/// Controller to delete a guard.
/// </summary>
[ApiVersion("1.0")]
public class DeleteGuardController : GuardsControllerBase
{
    private readonly IGuardService _guardService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteGuardController"/> class.
    /// </summary>
    /// <param name="guardService">Guard CRUD and lookup.</param>
    public DeleteGuardController(IGuardService guardService)
    {
        _guardService = guardService;
    }

    /// <summary>
    /// Delete a guard.
    /// </summary>
    /// <param name="key">The guard's key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete($"{{{nameof(key)}}}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteGuard(Guid key, CancellationToken cancellationToken = default)
    {
        var deleted = await _guardService.DeleteAsync(key, cancellationToken);
        return deleted ? Ok() : GuardNotFound();
    }
}
