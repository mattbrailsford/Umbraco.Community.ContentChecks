using Umbraco.Cms.Core.Models;
using Umbraco.Community.ContentChecks.Core.Checks;

namespace Umbraco.Community.ContentChecks.Core.Policies;

/// <summary>
/// Default implementation of <see cref="IContentCheckPublishGateService"/>. Reads the last stored
/// run for the content (never runs checks itself — see docs/guard-spec.md), reconciles it against
/// what should currently apply (a rule with no fresh result for it counts the same as a real
/// <see cref="ContentCheckStatus.NotEvaluated"/> — see <see cref="ReconcileAsync"/>), picks out
/// every entry that counts as failing, and resolves Guard fresh for the current user against each
/// one.
/// </summary>
internal sealed class ContentCheckPublishGateService : IContentCheckPublishGateService
{
    private readonly IContentCheckResultService _resultService;
    private readonly IContentCheckResolutionService _resolutionService;
    private readonly IGuardResolutionService _guardResolutionService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentCheckPublishGateService"/> class.
    /// </summary>
    /// <param name="resultService">Reads the content item's last stored run.</param>
    /// <param name="resolutionService">Resolves which rules should currently apply, so a rule with no fresh result isn't silently ignored.</param>
    /// <param name="guardResolutionService">Resolves whether a failing entry blocks the current user.</param>
    public ContentCheckPublishGateService(
        IContentCheckResultService resultService,
        IContentCheckResolutionService resolutionService,
        IGuardResolutionService guardResolutionService)
    {
        _resultService = resultService;
        _resolutionService = resolutionService;
        _guardResolutionService = guardResolutionService;
    }

    /// <inheritdoc />
    public async Task<ContentCheckPublishGateResult> EvaluateAsync(
        IContent content,
        string? culture,
        string? segment,
        Guid? currentUserKey,
        CancellationToken cancellationToken = default)
    {
        var failingEntries = await ReconcileAsync(content, culture, segment, cancellationToken);

        var blockingEntries = new List<ContentCheckPublishGateEntry>();
        foreach (var entry in failingEntries)
        {
            var response = await _guardResolutionService.ResolveResponseAsync(
                content, entry.RequirementLevel, currentUserKey, cancellationToken);

            if (response == ContentCheckResponse.Block)
            {
                blockingEntries.Add(entry);
            }
        }

        // Independent of Guard -- a Recommended-level miss is a fact about the content, not an
        // enforcement decision, so it's surfaced regardless of whether this user would be blocked
        // by anything. The caller decides how this combines with a block (see
        // ContentCheckPublishGateResult.RecommendedMissedEntries).
        var recommendedMissedEntries = failingEntries
            .Where(e => e.RequirementLevel == RequirementLevel.Recommended)
            .ToList();

        return new ContentCheckPublishGateResult
        {
            BlockingEntries = blockingEntries,
            RecommendedMissedEntries = recommendedMissedEntries,
        };
    }

    /// <summary>
    /// Reconciles what's currently resolved to apply against what was actually stored, and returns
    /// every entry that counts as failing. A resolved rule with no fresh matching stored result —
    /// nothing ever ran, or it ran against an earlier version of the content — is treated exactly
    /// like a real <see cref="ContentCheckStatus.NotEvaluated"/> result for that rule: it counts as
    /// failing only if the rule's own <see cref="ContentCheckNotEvaluatedPolicy"/> is
    /// <see cref="ContentCheckNotEvaluatedPolicy.FailClosed"/>. Without this reconciliation, a Guard
    /// that's meant to require a passing report could be dodged entirely just by publishing content
    /// that's never been checked, or was edited since it last was.
    /// </summary>
    private async Task<List<ContentCheckPublishGateEntry>> ReconcileAsync(
        IContent content, string? culture, string? segment, CancellationToken cancellationToken)
    {
        var storedResults = await _resultService.GetByContentKeyAsync(content.Key, culture, segment, cancellationToken);

        // Only a stored result that still matches the content as it currently stands is trusted —
        // a stale result (the content changed since it ran) is treated the same as no result at
        // all. See docs/guard-spec.md's "read a stored run matching the content as it stands."
        var currentStamp = new ContentCheckSnapshot(content).ContentStamp;
        var freshByLocation = storedResults
            .Where(r => r.ContentStamp == currentStamp)
            .ToLookup(r => (PathKey(r.Path), r.RuleKey));

        var targets = await _resolutionService.ResolveAsync(content, culture, segment, cancellationToken);

        var entries = new List<ContentCheckPublishGateEntry>();
        foreach (var target in targets)
        {
            var pathKey = PathKey(target.Path);
            // Keyed by RuleKey, not CheckAlias -- two rules can run the same check at the same
            // location with different config (e.g. two "required property" rules checking
            // different properties), and CheckAlias alone can't tell them apart.
            foreach (var rule in target.PolicyMatches.SelectMany(m => m.Rules))
            {
                var fresh = freshByLocation[(pathKey, rule.RuleKey)].FirstOrDefault();

                if (fresh is null)
                {
                    if (rule.NotEvaluatedPolicy == ContentCheckNotEvaluatedPolicy.FailClosed)
                    {
                        entries.Add(new ContentCheckPublishGateEntry
                        {
                            CheckAlias = rule.Check.Alias,
                            RequirementLevel = rule.RequirementLevel,
                            Message = "No check result exists for this content's current version — publish is blocked until this check has run against it.",
                        });
                    }

                    continue;
                }

                if (IsCountedAsFailing(fresh, rule.NotEvaluatedPolicy))
                {
                    entries.Add(new ContentCheckPublishGateEntry
                    {
                        CheckAlias = fresh.CheckAlias,
                        RequirementLevel = rule.RequirementLevel,
                        Message = fresh.Result.Message,
                    });
                }
            }
        }

        return entries;
    }

    /// <summary>
    /// An entry counts as failing if it's a real <see cref="ContentCheckStatus.Fail"/>, or it's
    /// <see cref="ContentCheckStatus.NotEvaluated"/> and the rule as currently configured is set to
    /// <see cref="ContentCheckNotEvaluatedPolicy.FailClosed"/>. The live rule's policy is used here
    /// rather than the snapshot stored on the result, so a policy edit made after the check last ran
    /// takes effect immediately instead of only after the content is re-checked. See
    /// docs/guard-spec.md's "NotEvaluatedPolicy decides whether a missed check counts as failing
    /// at all."
    /// </summary>
    private static bool IsCountedAsFailing(ContentCheckStoredResult result, ContentCheckNotEvaluatedPolicy currentNotEvaluatedPolicy) =>
        result.Result.Status == ContentCheckStatus.Fail
        || (result.Result.Status == ContentCheckStatus.NotEvaluated
            && currentNotEvaluatedPolicy == ContentCheckNotEvaluatedPolicy.FailClosed);

    /// <summary>
    /// A stable, order-sensitive key for a location path — <see cref="ContentCheckLocationStep"/>
    /// is a plain class with no structural equality of its own.
    /// </summary>
    private static string PathKey(IReadOnlyList<ContentCheckLocationStep> path) =>
        string.Join('/', path.Select(step => $"{step.PropertyAlias}:{step.ElementKey}"));
}
