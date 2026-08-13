using Umbraco.Cms.Core.Models;
using Umbraco.Community.ContentChecks.Core.Checks;

namespace Umbraco.Community.ContentChecks.Core.Policies;

/// <summary>
/// Default implementation of <see cref="IContentCheckEvaluationService"/>. Composes what already
/// exists and nothing more: resolves every target, runs each one through the unchanged
/// <see cref="IContentCheckRunner"/>, and zips each target's report entries with its
/// <see cref="ContentCheckResolvedTarget.Path"/> into one <see cref="ContentCheckAggregateReport"/>.
/// </summary>
internal sealed class ContentCheckEvaluationService : IContentCheckEvaluationService
{
    private readonly IContentCheckResolutionService _resolutionService;
    private readonly IContentCheckRunner _runner;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentCheckEvaluationService"/> class.
    /// </summary>
    /// <param name="resolutionService">Resolves every applicable target for a content item.</param>
    /// <param name="runner">Runs one target's resolved rules against its snapshot.</param>
    public ContentCheckEvaluationService(IContentCheckResolutionService resolutionService, IContentCheckRunner runner)
    {
        _resolutionService = resolutionService;
        _runner = runner;
    }

    /// <inheritdoc />
    public async Task<ContentCheckAggregateReport> EvaluateAsync(
        IContent content,
        string? culture = null,
        string? segment = null,
        IProgress<ContentCheckEvaluationProgress>? progress = null,
        Guid? currentUserKey = null,
        CancellationToken cancellationToken = default)
    {
        var targets = await _resolutionService.ResolveAsync(content, culture, segment, currentUserKey, cancellationToken);

        var entries = new List<ContentCheckAggregateReportEntry>();
        var targetsCompleted = 0;
        progress?.Report(new ContentCheckEvaluationProgress { TargetsTotal = targets.Count, TargetsCompleted = targetsCompleted });

        foreach (var target in targets)
        {
            var rules = target.PolicyMatches.SelectMany(m => m.Rules).ToList();
            var request = new ContentCheckRunRequest
            {
                Content = target.Snapshot,
                Culture = culture,
                Segment = segment,
                Rules = rules,
            };

            var report = await _runner.RunAsync(request, cancellationToken);
            entries.AddRange(report.Entries.Select(entry => new ContentCheckAggregateReportEntry
            {
                Entry = entry,
                Path = target.Path,
            }));

            targetsCompleted++;
            progress?.Report(new ContentCheckEvaluationProgress { TargetsTotal = targets.Count, TargetsCompleted = targetsCompleted });
        }

        return new ContentCheckAggregateReport
        {
            ContentKey = content.Key,
            Entries = entries,
        };
    }
}
