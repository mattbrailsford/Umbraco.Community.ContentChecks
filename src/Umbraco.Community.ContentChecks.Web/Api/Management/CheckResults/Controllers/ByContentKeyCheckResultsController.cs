using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Community.ContentChecks.Core.Checks;
using Umbraco.Community.ContentChecks.Core.Policies;

namespace Umbraco.Community.ContentChecks.Web.Api.Management.CheckResults.Controllers;

/// <summary>
/// Controller to get a content item's persisted check results — the last completed run's own
/// output, read without needing a run currently in flight. Returns the same
/// <see cref="ContentCheckAggregateReport"/> shape a live run streams, so the workspace view can
/// render a stored result and a live one identically.
/// </summary>
[ApiVersion("1.0")]
public class ByContentKeyCheckResultsController : CheckResultsControllerBase
{
    private readonly IContentCheckResultService _resultService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ByContentKeyCheckResultsController"/> class.
    /// </summary>
    /// <param name="resultService">Reads persisted results.</param>
    public ByContentKeyCheckResultsController(IContentCheckResultService resultService)
    {
        _resultService = resultService;
    }

    /// <summary>
    /// Get a content item's persisted check results from its most recently completed run.
    /// </summary>
    /// <param name="contentKey">The content item's key.</param>
    /// <param name="culture">The culture the run was evaluated for.</param>
    /// <param name="segment">The segment the run was evaluated for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The persisted report — empty entries if this item has never been evaluated.</returns>
    [HttpGet($"{{{nameof(contentKey)}}}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(ContentCheckAggregateReport), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByContentKey(
        Guid contentKey, string? culture = null, string? segment = null, CancellationToken cancellationToken = default)
    {
        var results = await _resultService.GetByContentKeyAsync(contentKey, culture, segment, cancellationToken);

        var report = new ContentCheckAggregateReport
        {
            ContentKey = contentKey,
            ContentStamp = results.FirstOrDefault()?.ContentStamp,
            Entries = results.Select(r => new ContentCheckAggregateReportEntry
            {
                Entry = new ContentCheckReportEntry
                {
                    CheckAlias = r.CheckAlias,
                    RequirementLevel = r.RequirementLevel,
                    Result = r.Result,
                },
                Path = r.Path,
            }).ToList(),
        };

        return Ok(report);
    }
}
