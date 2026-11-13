using System.ComponentModel;
using Umbraco.AI.Core.Tools;
using Umbraco.Cms.Core.Services;
using Umbraco.Community.ContentChecks.Core.Policies;

namespace Umbraco.Community.ContentChecks.AI.Tools;

/// <summary>
/// Arguments for the run_content_checks tool.
/// </summary>
public record RunContentChecksArgs(
    [property: Description("The unique key (GUID) of the content item to run checks against.")]
    Guid ContentKey,

    [property: Description("Optional culture to evaluate variant properties for (e.g. 'en-US'). Omit for invariant content.")]
    string? Culture = null,

    [property: Description("Optional segment to evaluate segmented properties for. Omit unless the content type uses segmentation.")]
    string? Segment = null);

/// <summary>
/// Tool that runs every applicable check policy against a content item and returns the combined
/// report -- the "run the checks and give me a report" half of docs/overview.md §2's non-goal
/// boundary; the calling agent is the one that iterates.
/// </summary>
/// <remarks>
/// One of Content Checks' two ways in to Umbraco.AI (docs/overview.md §6): this tool calls the
/// runner in-process for a site that also runs Umbraco.AI directly; a site without Umbraco.AI
/// installed would instead reach the same evaluation through the Management API via the Umbraco
/// MCP.
/// </remarks>
[AITool("run_content_checks", "Run Content Checks", ScopeId = ContentChecksToolScope.ScopeId)]
public sealed class RunContentChecksTool : AIToolBase<RunContentChecksArgs>
{
    private readonly IContentService _contentService;
    private readonly IContentCheckEvaluationService _evaluationService;
    private readonly IContentCheckAgentRoundTracker _roundTracker;

    /// <summary>
    /// Initializes a new instance of the <see cref="RunContentChecksTool"/> class.
    /// </summary>
    public RunContentChecksTool(
        IContentService contentService,
        IContentCheckEvaluationService evaluationService,
        IContentCheckAgentRoundTracker roundTracker)
    {
        _contentService = contentService;
        _evaluationService = evaluationService;
        _roundTracker = roundTracker;
    }

    /// <inheritdoc />
    public override string Description =>
        "Runs every applicable content-check policy against a content item and returns a report " +
        "(pass/warn/fail per rule, with feedback). Use this to validate content you've just " +
        "generated or edited, and re-run it after fixing anything it flags. This tool enforces a " +
        "hard cap on how many times it will re-run for the same content item in a row -- once you " +
        "hit it, stop iterating and report the remaining issues to a human instead of calling this " +
        "again.";

    /// <inheritdoc />
    protected override async Task<object> ExecuteAsync(RunContentChecksArgs args, CancellationToken cancellationToken = default)
    {
        var content = _contentService.GetById(args.ContentKey);
        if (content is null)
        {
            return new RunContentChecksResult(
                Success: false,
                ContentKey: args.ContentKey,
                Round: 0,
                MaxRounds: 0,
                RoundCapReached: false,
                OverallStatus: null,
                Entries: [],
                Message: $"No content item found with key '{args.ContentKey}'.");
        }

        // Loop discipline (finding G2): the cap is enforced here, as a hard limit, not left to an
        // agent's system prompt to self-police. See docs/ai-integration-spec.md's build checklist.
        var round = _roundTracker.RecordRound(args.ContentKey, args.Culture, args.Segment);
        if (round.CapReached)
        {
            return new RunContentChecksResult(
                Success: false,
                ContentKey: args.ContentKey,
                Round: round.Round,
                MaxRounds: round.MaxRounds,
                RoundCapReached: true,
                OverallStatus: null,
                Entries: [],
                Message: $"This content item has already been checked {round.MaxRounds} times in a row without converging. " +
                          "Stop iterating -- report the remaining issues to a human instead of running checks again.");
        }

        var report = await _evaluationService.EvaluateAsync(
            content, args.Culture, args.Segment, cancellationToken: cancellationToken);

        var entries = report.Entries
            .Select(e => new RunContentChecksEntry(
                CheckAlias: e.Entry.CheckAlias,
                RequirementLevel: e.Entry.RequirementLevel.ToString(),
                Status: e.Entry.Result.Status.ToString(),
                Message: e.Entry.Result.Message,
                Score: e.Entry.Result.Score,
                Threshold: e.Entry.Result.Threshold,
                Location: e.Path.Select(step => step.PropertyAlias).ToList()))
            .ToList();

        return new RunContentChecksResult(
            Success: true,
            ContentKey: args.ContentKey,
            Round: round.Round,
            MaxRounds: round.MaxRounds,
            RoundCapReached: false,
            OverallStatus: report.OverallStatus.ToString(),
            Entries: entries,
            Message: null);
    }
}

/// <summary>
/// Result of the run_content_checks tool.
/// </summary>
public record RunContentChecksResult(
    bool Success,
    Guid ContentKey,
    int Round,
    int MaxRounds,
    bool RoundCapReached,
    string? OverallStatus,
    IReadOnlyList<RunContentChecksEntry> Entries,
    string? Message);

/// <summary>
/// One check result within a run_content_checks report.
/// </summary>
public record RunContentChecksEntry(
    string CheckAlias,
    string RequirementLevel,
    string Status,
    string Message,
    double? Score,
    double? Threshold,
    IReadOnlyList<string> Location);
