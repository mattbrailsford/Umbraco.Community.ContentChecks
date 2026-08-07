using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Community.ContentChecks.Web.Api.Management.Evaluation.Models;
using Umbraco.Community.ContentChecks.Web.Runs;

namespace Umbraco.Community.ContentChecks.Web.Api.Management.Evaluation.Controllers;

/// <summary>
/// Controller to start a run — one or more content items, checked in the background. A single
/// content key is just the N=1 case of the same mechanism, not a separate endpoint.
/// </summary>
[ApiVersion("1.0")]
public class StartEvaluationController : EvaluationControllerBase
{
    private readonly IContentCheckRunTracker _tracker;
    private readonly IContentCheckRunQueue _queue;

    /// <summary>
    /// Initializes a new instance of the <see cref="StartEvaluationController"/> class.
    /// </summary>
    /// <param name="tracker">The live-run registry.</param>
    /// <param name="queue">Queues a newly-started run for the background worker.</param>
    public StartEvaluationController(IContentCheckRunTracker tracker, IContentCheckRunQueue queue)
    {
        _tracker = tracker;
        _queue = queue;
    }

    /// <summary>
    /// Start a run against one or more content items. Returns immediately, before any content
    /// has been checked — poll <see cref="GetEvaluationRunController.GetRun"/> or watch
    /// <see cref="StreamEvaluationRunController.StreamRun"/> for progress.
    /// </summary>
    /// <param name="requestModel">The content items to evaluate.</param>
    /// <returns>The new run's id.</returns>
    [HttpPost]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(StartEvaluationResponseModel), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult StartEvaluation(StartEvaluationRequestModel requestModel)
    {
        if (requestModel.ContentKeys.Count == 0)
        {
            return BadRequest(CreateProblemDetails(
                "No content keys",
                "At least one content key is required."));
        }

        if (requestModel.ContentKeys.Count > Constants.ManagementApi.Feature.Evaluation.MaxBatchSize)
        {
            var detail = $"A single run is capped at {Constants.ManagementApi.Feature.Evaluation.MaxBatchSize} content items.";
            return BadRequest(CreateProblemDetails("Too many content keys", detail));
        }

        var run = _tracker.StartRun(requestModel.ContentKeys);
        _queue.Enqueue(run.RunId);

        return CreatedAtAction(
            nameof(GetEvaluationRunController.GetRun),
            nameof(GetEvaluationRunController).Replace("Controller", string.Empty),
            new { runId = run.RunId },
            new StartEvaluationResponseModel { RunId = run.RunId });
    }
}
