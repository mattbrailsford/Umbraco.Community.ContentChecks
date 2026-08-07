using System.Text.Json;
using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Community.ContentChecks.Web.Api.Management.Evaluation.Models;
using Umbraco.Community.ContentChecks.Web.Runs;

namespace Umbraco.Community.ContentChecks.Web.Api.Management.Evaluation.Controllers;

/// <summary>
/// Controller to watch a run live over Server-Sent Events — one consistent shape regardless of
/// how many items the run has. Emits an event every time any item's state changes, and a final
/// event once every item has finished, then closes the stream.
/// </summary>
[ApiVersion("1.0")]
public class StreamEvaluationRunController : EvaluationControllerBase
{
    /// <summary>
    /// How often the stream re-checks the tracker for changes. The tracker is an in-memory
    /// dictionary, so polling it is cheap -- this is not a network round-trip.
    /// </summary>
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(500);

    private readonly IContentCheckRunTracker _tracker;
    private readonly IUmbracoMapper _umbracoMapper;
    private readonly IOptionsMonitor<JsonOptions> _jsonOptionsMonitor;

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamEvaluationRunController"/> class.
    /// </summary>
    /// <param name="tracker">The live-run registry.</param>
    /// <param name="umbracoMapper">Maps run state to API response models.</param>
    /// <param name="jsonOptionsMonitor">Resolves this API's own named JSON serializer options.</param>
    public StreamEvaluationRunController(
        IContentCheckRunTracker tracker, IUmbracoMapper umbracoMapper, IOptionsMonitor<JsonOptions> jsonOptionsMonitor)
    {
        _tracker = tracker;
        _umbracoMapper = umbracoMapper;
        _jsonOptionsMonitor = jsonOptionsMonitor;
    }

    /// <summary>
    /// Watch a run live. Not a typical formatted response — writes directly to the response body
    /// as <c>text/event-stream</c>, so there's no <see cref="ProducesResponseTypeAttribute"/> body
    /// type to declare beyond the stream itself.
    /// </summary>
    /// <param name="runId">The run's id.</param>
    /// <param name="cancellationToken">Cancellation token -- ends the stream when the client disconnects.</param>
    /// <returns>A task that completes when the stream ends.</returns>
    [HttpGet($"{{{nameof(runId)}}}/stream")]
    [MapToApiVersion("1.0")]
    public async Task StreamRun(Guid runId, CancellationToken cancellationToken)
    {
        var run = _tracker.GetRun(runId);
        if (run is null)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var jsonOptions = _jsonOptionsMonitor.Get(Constants.ManagementApi.ApiName).JsonSerializerOptions;

        Response.Headers.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";

        var lastSentJsonByContentKey = new Dictionary<Guid, string>();

        while (!cancellationToken.IsCancellationRequested)
        {
            foreach (var item in run.Items.Values)
            {
                var itemJson = JsonSerializer.Serialize(_umbracoMapper.Map<EvaluationItemResponseModel>(item), jsonOptions);
                if (lastSentJsonByContentKey.TryGetValue(item.ContentKey, out var previousJson) && previousJson == itemJson)
                {
                    continue;
                }

                lastSentJsonByContentKey[item.ContentKey] = itemJson;
                await WriteEventAsync("progress", itemJson, cancellationToken);
            }

            if (run.Status == ContentCheckRunStatus.Completed)
            {
                var runJson = JsonSerializer.Serialize(_umbracoMapper.Map<EvaluationRunResponseModel>(run), jsonOptions);
                await WriteEventAsync("complete", runJson, cancellationToken);
                break;
            }

            await Task.Delay(PollInterval, cancellationToken);
        }
    }

    private async Task WriteEventAsync(string eventName, string dataJson, CancellationToken cancellationToken)
    {
        await Response.WriteAsync($"event: {eventName}\ndata: {dataJson}\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }
}
