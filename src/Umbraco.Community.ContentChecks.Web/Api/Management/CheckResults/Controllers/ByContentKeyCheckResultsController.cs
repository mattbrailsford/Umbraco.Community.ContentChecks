using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Services;
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
    private readonly IContentService _contentService;
    private readonly IContentCheckResultService _resultService;
    private readonly IContentCheckResolutionService _resolutionService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ByContentKeyCheckResultsController"/> class.
    /// </summary>
    /// <param name="contentService">Resolves a content item by key.</param>
    /// <param name="resultService">Reads persisted results.</param>
    /// <param name="resolutionService">Resolves each rule's current <see cref="RequirementLevel"/>, so an edit to a policy after a check last ran is reflected immediately instead of only after the content is re-checked.</param>
    public ByContentKeyCheckResultsController(
        IContentService contentService,
        IContentCheckResultService resultService,
        IContentCheckResolutionService resolutionService)
    {
        _contentService = contentService;
        _resultService = resultService;
        _resolutionService = resolutionService;
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
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByContentKey(
        Guid contentKey, string? culture = null, string? segment = null, CancellationToken cancellationToken = default)
    {
        var content = _contentService.GetById(contentKey);
        if (content is null)
        {
            return ContentNotFound();
        }

        var results = await _resultService.GetByContentKeyAsync(contentKey, culture, segment, cancellationToken);

        // A stored result no longer carries its own RequirementLevel/NotEvaluatedPolicy -- those
        // are resolved live here, the same way the publish gate does it, so a policy edit takes
        // effect immediately rather than only after the content is re-checked. A stored row whose
        // rule/path no longer matches any currently-resolved rule (e.g. the rule was deleted) has
        // nowhere to look up a level for, so it's dropped from the report entirely rather than
        // shown with a nonsense level.
        // Keyed by RuleKey rather than CheckAlias -- two rules can run the same check at the same
        // location with different config (e.g. two "required property" rules checking different
        // properties), and CheckAlias alone can't tell them apart. Grouped rather than a straight
        // ToDictionary because the same rule can still be reached through more than one policy
        // match at the same location (e.g. two policies both carrying the identical rule); those
        // collapse safely since they share the same RuleKey and therefore the same RequirementLevel.
        var targets = await _resolutionService.ResolveAsync(content, culture, segment, cancellationToken);
        var levelByLocation = targets
            .SelectMany(target => target.PolicyMatches
                .SelectMany(m => m.Rules)
                .Select(rule => (PathKey: PathKey(target.Path), rule.RuleKey, rule.RequirementLevel)))
            .GroupBy(x => (x.PathKey, x.RuleKey))
            .ToDictionary(g => g.Key, g => g.Max(x => x.RequirementLevel));

        var report = new ContentCheckAggregateReport
        {
            ContentKey = contentKey,
            ContentStamp = results.FirstOrDefault()?.ContentStamp,
            Entries = results
                .Where(r => levelByLocation.ContainsKey((PathKey(r.Path), r.RuleKey)))
                .Select(r => new ContentCheckAggregateReportEntry
                {
                    Entry = new ContentCheckReportEntry
                    {
                        CheckAlias = r.CheckAlias,
                        RuleKey = r.RuleKey,
                        RequirementLevel = levelByLocation[(PathKey(r.Path), r.RuleKey)],
                        Result = r.Result,
                    },
                    Path = r.Path,
                }).ToList(),
        };

        return Ok(report);
    }

    /// <summary>
    /// A stable, order-sensitive key for a location path — <see cref="ContentCheckLocationStep"/>
    /// is a plain class with no structural equality of its own. Mirrors
    /// <c>ContentCheckPublishGateService.PathKey</c>, which does the same live-rule matching for
    /// the same reason.
    /// </summary>
    private static string PathKey(IReadOnlyList<ContentCheckLocationStep> path) =>
        string.Join('/', path.Select(step => $"{step.PropertyAlias}:{step.ElementKey}"));
}
