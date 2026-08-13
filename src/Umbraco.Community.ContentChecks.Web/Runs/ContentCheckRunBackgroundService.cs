using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Core.Services;
using Umbraco.Community.ContentChecks.Core.Checks;
using Umbraco.Community.ContentChecks.Core.Policies;
using Umbraco.Community.ContentChecks.Web.Api.Management.Evaluation.Models;

namespace Umbraco.Community.ContentChecks.Web.Runs;

/// <summary>
/// Drains <see cref="IContentCheckRunQueue"/> and runs each queued run's content items through
/// <see cref="IContentCheckEvaluationService"/>. Never touches the DI scope of the request that
/// enqueued the run — a bare <c>Task.Run</c> inside the request handler is a known trap, since
/// that scope (and anything scoped it was using) gets torn down the moment the response is sent.
/// </summary>
internal sealed class ContentCheckRunBackgroundService : BackgroundService
{
    /// <summary>
    /// Each item's own evaluation can already fire up to 4 concurrent AI-judged checks internally
    /// (the runner's own cap, unchanged). Running every item in a run fully in parallel on top of
    /// that multiplies unpredictably, so items within one run get their own small, separate cap —
    /// the same bounded-concurrency pattern the runner already uses for checks, one layer up.
    /// </summary>
    private const int MaxConcurrentItemsPerRun = 3;

    private readonly IContentCheckRunQueue _queue;
    private readonly IContentCheckRunTracker _tracker;
    private readonly IContentCheckRunNotifier _notifier;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ContentCheckRunBackgroundService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentCheckRunBackgroundService"/> class.
    /// </summary>
    /// <param name="queue">The queue of runs waiting to be processed.</param>
    /// <param name="tracker">The live-run registry to read and update.</param>
    /// <param name="notifier">Pushes each change to whichever backoffice clients are watching the run.</param>
    /// <param name="scopeFactory">Creates a fresh DI scope per content item, never the request's own.</param>
    /// <param name="logger">Logs a failed item's evaluation.</param>
    public ContentCheckRunBackgroundService(
        IContentCheckRunQueue queue,
        IContentCheckRunTracker tracker,
        IContentCheckRunNotifier notifier,
        IServiceScopeFactory scopeFactory,
        ILogger<ContentCheckRunBackgroundService> logger)
    {
        _queue = queue;
        _tracker = tracker;
        _notifier = notifier;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            Guid runId;
            try
            {
                runId = await _queue.DequeueAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            // Not awaited: a run's own items are bounded by MaxConcurrentItemsPerRun, but nothing
            // stops two different runs' worth of items from proceeding at once -- the cap here is
            // per-run, not a single global one, since nothing in this project's design has asked
            // for the latter yet.
            _ = ProcessRunAsync(runId, stoppingToken);
        }
    }

    private async Task ProcessRunAsync(Guid runId, CancellationToken cancellationToken)
    {
        var run = _tracker.GetRun(runId);
        if (run is null)
        {
            return;
        }

        using var throttle = new SemaphoreSlim(MaxConcurrentItemsPerRun);
        await Task.WhenAll(run.ContentKeys.Select(contentKey => ProcessItemAsync(run, contentKey, throttle, cancellationToken)));

        // Every item has finished by construction of the WhenAll above, so the run itself is
        // always Completed here -- this is the one event a client needs even if it missed every
        // individual item's progress, since it's what lets a watcher stop waiting.
        using var scope = _scopeFactory.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IUmbracoMapper>();
        await _notifier.NotifyCompleteAsync(mapper.Map<EvaluationRunResponseModel>(run)!, cancellationToken);
    }

    private async Task ProcessItemAsync(ContentCheckRunState run, Guid contentKey, SemaphoreSlim throttle, CancellationToken cancellationToken)
    {
        await throttle.WaitAsync(cancellationToken);
        try
        {
            // A fresh scope per item, created here rather than reused across the run's other
            // items -- Umbraco's own scoped services (content, EF Core repositories) aren't
            // designed to be shared across concurrently-running work.
            using var scope = _scopeFactory.CreateScope();
            var contentService = scope.ServiceProvider.GetRequiredService<IContentService>();
            var evaluationService = scope.ServiceProvider.GetRequiredService<IContentCheckEvaluationService>();
            var resultService = scope.ServiceProvider.GetRequiredService<IContentCheckResultService>();
            var mapper = scope.ServiceProvider.GetRequiredService<IUmbracoMapper>();

            var content = contentService.GetById(contentKey);
            if (content is null)
            {
                run.FailItem(contentKey, "The content item could not be found.");
                await NotifyItemChangedAsync(mapper, run, contentKey, cancellationToken);
                return;
            }

            // Progress<T>'s callback is synchronous, so the notification is fired without
            // awaiting it -- these are best-effort UI updates; a dropped one is fixed up by the
            // "complete" event (or a hub reconnect's catch-up replay) either way.
            var progress = new Progress<ContentCheckEvaluationProgress>(p =>
            {
                run.UpdateProgress(contentKey, p);
                _ = NotifyItemChangedAsync(mapper, run, contentKey, cancellationToken);
            });
            var report = await evaluationService.EvaluateAsync(content, progress: progress, cancellationToken: cancellationToken);

            // Persisted only once the run for this item has fully succeeded -- a run that fails
            // partway through must never wipe out a previous good run's results, so "clear before a
            // run" is really "replace once new results exist," applied here rather than up front.
            var ranAtUtc = DateTime.UtcNow;
            var storedResults = report.Entries.Select(entry => new ContentCheckStoredResult
            {
                ContentKey = contentKey,
                ContentStamp = report.ContentStamp,
                Path = entry.Path,
                CheckAlias = entry.Entry.CheckAlias,
                RuleKey = entry.Entry.RuleKey,
                Result = entry.Entry.Result,
                RanAtUtc = ranAtUtc,
            }).ToList();
            await resultService.ReplaceForContentAsync(contentKey, culture: null, segment: null, storedResults, cancellationToken);

            run.CompleteItem(contentKey, report);
            await NotifyItemChangedAsync(mapper, run, contentKey, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Content check evaluation failed for content {ContentKey} in run {RunId}", contentKey, run.RunId);
            run.FailItem(contentKey, ex.Message);

            using var scope = _scopeFactory.CreateScope();
            var mapper = scope.ServiceProvider.GetRequiredService<IUmbracoMapper>();
            await NotifyItemChangedAsync(mapper, run, contentKey, cancellationToken);
        }
        finally
        {
            throttle.Release();
        }
    }

    private Task NotifyItemChangedAsync(IUmbracoMapper mapper, ContentCheckRunState run, Guid contentKey, CancellationToken cancellationToken)
    {
        if (!run.Items.TryGetValue(contentKey, out var item))
        {
            return Task.CompletedTask;
        }

        return _notifier.NotifyItemChangedAsync(run.RunId, mapper.Map<EvaluationItemResponseModel>(item)!, cancellationToken);
    }
}
