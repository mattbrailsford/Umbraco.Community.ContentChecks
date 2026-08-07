using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Community.ContentChecks.Web.Api.Management.Evaluation.Models;
using Umbraco.Community.ContentChecks.Web.Runs;

namespace Umbraco.Community.ContentChecks.Web.Api.Management.Evaluation.Controllers;

/// <summary>
/// Controller to poll a run's current state — a plain "state right now" read of the tracker,
/// free once it exists, and a fallback if SSE is ever blocked (some proxies still mishandle
/// long-lived connections).
/// </summary>
[ApiVersion("1.0")]
public class GetEvaluationRunController : EvaluationControllerBase
{
    private readonly IContentCheckRunTracker _tracker;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetEvaluationRunController"/> class.
    /// </summary>
    /// <param name="tracker">The live-run registry.</param>
    /// <param name="umbracoMapper">Maps run state to API response models.</param>
    public GetEvaluationRunController(IContentCheckRunTracker tracker, IUmbracoMapper umbracoMapper)
    {
        _tracker = tracker;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Get a run's current state.
    /// </summary>
    /// <param name="runId">The run's id.</param>
    /// <returns>The run's current state.</returns>
    [HttpGet($"{{{nameof(runId)}}}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(EvaluationRunResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public IActionResult GetRun(Guid runId)
    {
        var run = _tracker.GetRun(runId);
        return run is null ? RunNotFound() : Ok(_umbracoMapper.Map<EvaluationRunResponseModel>(run));
    }
}
