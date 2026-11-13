using System.Diagnostics;
using Umbraco.Cms.Core.Events;
using Umbraco.Community.ContentChecks.Core.Markup;

namespace Umbraco.Community.ContentChecks.Core.Checks;

/// <summary>
/// Default implementation of <see cref="IContentCheckRunner"/>.
/// </summary>
/// <remarks>
/// Ordering keys off <see cref="ContentCheckCost"/>, not <see cref="ContentCheckKind"/> — whether
/// a check's output is reproducible is a cache/disclosure concern; whether it's cheap to run is
/// what scheduling actually needs:
/// <list type="number">
/// <item>Every <c>Free</c> rule runs first, always.</item>
/// <item>If any <c>Free</c> rule at <see cref="RequirementLevel.Mandatory"/> produced <see cref="ContentCheckStatus.Fail"/>,
/// every <c>Metered</c> rule short-circuits straight to <see cref="ContentCheckNotEvaluatedReason.ShortCircuited"/>
/// — no expensive call is made. This is a cost decision, not an enforcement one: a run never
/// resolves who a failure blocks (see docs/guard-spec.md), so it short-circuits on how important a
/// failing rule is declared to be, not on whether it would go on to block anyone.</item>
/// <item>Otherwise every <c>Metered</c> rule runs, concurrently, up to a per-run cap.</item>
/// </list>
/// </remarks>
internal sealed class ContentCheckRunner : IContentCheckRunner
{
    private const int MaxConcurrentMeteredChecks = 4;

    private readonly IContentCheckResultCache _cache;
    private readonly IEventAggregator _eventAggregator;
    private readonly IContentCheckMarkupProvider _markupProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentCheckRunner"/> class.
    /// </summary>
    /// <param name="cache">The result cache.</param>
    /// <param name="eventAggregator">Publishes <see cref="ContentCheckReportCompletedNotification"/> after each run.</param>
    /// <param name="markupProvider">Fetches markup for any <see cref="IMarkupContentCheck"/> rule — see docs/markup-checks-exploration.md §4.2.</param>
    public ContentCheckRunner(IContentCheckResultCache cache, IEventAggregator eventAggregator, IContentCheckMarkupProvider markupProvider)
    {
        _cache = cache;
        _eventAggregator = eventAggregator;
        _markupProvider = markupProvider;
    }

    /// <inheritdoc />
    public async Task<ContentCheckReport> RunAsync(ContentCheckRunRequest request, CancellationToken cancellationToken)
    {
        var freeRules = request.Rules.Where(r => r.Check.Cost == ContentCheckCost.Free).ToList();
        var meteredRules = request.Rules.Where(r => r.Check.Cost == ContentCheckCost.Metered).ToList();

        // Fetched at most once per run, lazily -- only triggered the first time a rule that's
        // actually an IMarkupContentCheck runs (never eagerly here), and shared by every markup
        // rule in this run via Lazy<Task<T>>'s own thread-safe caching. A page with no markup
        // rules never fetches at all; a markup rule skipped by the mandatory-Free-failure
        // short-circuit below never triggers it either (docs/markup-checks-exploration.md §4.2).
        var markupFetch = request.Rules.Any(r => r.Check is IMarkupContentCheck)
            ? new Lazy<Task<ContentCheckMarkupResult>>(() => _markupProvider.GetMarkupAsync(
                new ContentCheckMarkupRequest
                {
                    ContentKey = request.Content.Key,
                    Culture = request.Culture,
                    Segment = request.Segment,
                },
                cancellationToken))
            : null;

        var entries = new List<ContentCheckReportEntry>(request.Rules.Count);

        foreach (var rule in freeRules)
        {
            entries.Add(await RunRuleAsync(rule, request, markupFetch, cancellationToken));
        }

        var mandatoryFreeFailure = entries.Any(e =>
            e.RequirementLevel == RequirementLevel.Mandatory && e.Result.Status == ContentCheckStatus.Fail);

        if (mandatoryFreeFailure)
        {
            entries.AddRange(meteredRules.Select(rule => new ContentCheckReportEntry
            {
                RuleKey = rule.RuleKey,
                CheckAlias = rule.Check.Alias,
                RequirementLevel = rule.RequirementLevel,
                NotEvaluatedPolicy = rule.NotEvaluatedPolicy,
                Result = new ContentCheckResult
                {
                    Status = ContentCheckStatus.NotEvaluated,
                    Message = "Skipped: a blocking check already failed.",
                    NotEvaluatedReason = ContentCheckNotEvaluatedReason.ShortCircuited,
                },
            }));
        }
        else if (meteredRules.Count > 0)
        {
            using var throttle = new SemaphoreSlim(MaxConcurrentMeteredChecks);
            var meteredEntries = await Task.WhenAll(meteredRules.Select(rule =>
                RunThrottledRuleAsync(rule, request, markupFetch, throttle, cancellationToken)));
            entries.AddRange(meteredEntries);
        }

        var report = new ContentCheckReport { Entries = entries };

        await _eventAggregator.PublishAsync(
            new ContentCheckReportCompletedNotification
            {
                Report = report,
                ContentKey = request.Content.Key,
                ContentTypeAlias = request.Content.ContentTypeAlias,
                Culture = request.Culture,
                Segment = request.Segment,
            },
            cancellationToken);

        return report;
    }

    private async Task<ContentCheckReportEntry> RunThrottledRuleAsync(
        ResolvedContentCheckRule rule,
        ContentCheckRunRequest request,
        Lazy<Task<ContentCheckMarkupResult>>? markupFetch,
        SemaphoreSlim throttle,
        CancellationToken cancellationToken)
    {
        await throttle.WaitAsync(cancellationToken);
        try
        {
            return await RunRuleAsync(rule, request, markupFetch, cancellationToken);
        }
        finally
        {
            throttle.Release();
        }
    }

    private async Task<ContentCheckReportEntry> RunRuleAsync(
        ResolvedContentCheckRule rule,
        ContentCheckRunRequest request,
        Lazy<Task<ContentCheckMarkupResult>>? markupFetch,
        CancellationToken cancellationToken)
    {
        var cacheKey = BuildCacheKey(rule, request);

        if (cacheKey is not null)
        {
            var cached = await _cache.GetAsync(cacheKey, cancellationToken);
            if (cached is not null)
            {
                return new ContentCheckReportEntry
                {
                    RuleKey = rule.RuleKey,
                    CheckAlias = rule.Check.Alias,
                    RequirementLevel = rule.RequirementLevel,
                    NotEvaluatedPolicy = rule.NotEvaluatedPolicy,
                    Result = cached,
                    Elapsed = TimeSpan.Zero,
                };
            }
        }

        var stopwatch = Stopwatch.StartNew();
        ContentCheckResult result;
        try
        {
            result = rule.Check switch
            {
                IMarkupContentCheck markupCheck => await markupCheck.EvaluateAsync(
                    new ContentCheckMarkupContext
                    {
                        Content = request.Content,
                        Culture = request.Culture,
                        Segment = request.Segment,
                        Markup = await markupFetch!.Value,
                    },
                    rule.Config,
                    cancellationToken),
                IPropertyContentCheck propertyCheck => await propertyCheck.EvaluateAsync(
                    new ContentCheckContext
                    {
                        Content = request.Content,
                        Culture = request.Culture,
                        Segment = request.Segment,
                    },
                    rule.Config,
                    cancellationToken),
                _ => throw new InvalidOperationException(
                    $"Check '{rule.Check.Alias}' is neither an {nameof(IPropertyContentCheck)} nor an {nameof(IMarkupContentCheck)}."),
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            result = new ContentCheckResult
            {
                Status = ContentCheckStatus.NotEvaluated,
                Message = $"The check threw: {ex.Message}",
                NotEvaluatedReason = ContentCheckNotEvaluatedReason.Error,
            };
        }

        stopwatch.Stop();

        if (cacheKey is not null)
        {
            await _cache.SetAsync(cacheKey, result, cancellationToken);
        }

        return new ContentCheckReportEntry
        {
            RuleKey = rule.RuleKey,
            CheckAlias = rule.Check.Alias,
            RequirementLevel = rule.RequirementLevel,
            NotEvaluatedPolicy = rule.NotEvaluatedPolicy,
            Result = result,
            Elapsed = stopwatch.Elapsed,
        };
    }

    /// <summary>
    /// Builds the cache key for one rule against one snapshot, or <c>null</c> if the snapshot can't
    /// say when its content last changed — in which case it isn't cached at all, rather than being
    /// cached against something that doesn't actually track change.
    /// <para>
    /// Keys on <see cref="IContentCheckSnapshot.ContentStamp"/>, never
    /// <see cref="IContentCheckSnapshot.VersionId"/>: a draft save changes values in place without
    /// touching the version id, so a version-keyed entry returns the pre-edit verdict for post-edit
    /// content.
    /// </para>
    /// </summary>
    private static string? BuildCacheKey(ResolvedContentCheckRule rule, ContentCheckRunRequest request)
    {
        if (request.Content.ContentStamp is not { } contentStamp)
        {
            return null;
        }

        var assemblyVersion = rule.Check.GetType().Assembly.GetName().Version;
        var configJson = rule.Config.Config?.GetRawText() ?? string.Empty;

        return string.Join(
            '|',
            contentStamp,
            request.Content.Key,
            request.Culture ?? string.Empty,
            request.Segment ?? string.Empty,
            rule.Check.Alias,
            assemblyVersion,
            configJson);
    }
}
