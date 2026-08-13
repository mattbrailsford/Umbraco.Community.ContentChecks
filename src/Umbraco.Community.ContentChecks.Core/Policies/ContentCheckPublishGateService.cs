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

        var blockingEntries = new List<ContentCheckPublishGateBlockingEntry>();
        foreach (var entry in failingEntries)
        {
            var response = await _guardResolutionService.ResolveResponseAsync(
                content, entry.RequirementLevel, currentUserKey, cancellationToken);

            if (response == ContentCheckResponse.Block)
            {
                blockingEntries.Add(entry);
            }
        }

        return new ContentCheckPublishGateResult { BlockingEntries = blockingEntries };
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
    private async Task<List<ContentCheckPublishGateBlockingEntry>> ReconcileAsync(
        IContent content, string? culture, string? segment, CancellationToken cancellationToken)
    {
        var storedResults = await _resultService.GetByContentKeyAsync(content.Key, culture, segment, cancellationToken);

        // Only a stored result that still matches the content as it currently stands is trusted —
        // a stale result (the content changed since it ran) is treated the same as no result at
        // all. See docs/guard-spec.md's "read a stored run matching the content as it stands."
        var currentStamp = new ContentCheckSnapshot(content).ContentStamp;
        var freshByLocation = storedResults
            .Where(r => r.ContentStamp == currentStamp)
            .ToLookup(r => (PathKey(r.Path), r.CheckAlias));

        var targets = await _resolutionService.ResolveAsync(content, culture, segment, cancellationToken);

        var entries = new List<ContentCheckPublishGateBlockingEntry>();
        foreach (var target in targets)
        {
            var pathKey = PathKey(target.Path);
            foreach (var rule in target.PolicyMatches.SelectMany(m => m.Rules))
            {
                var fresh = freshByLocation[(pathKey, rule.Check.Alias)].FirstOrDefault();

                if (fresh is null)
                {
                    if (rule.NotEvaluatedPolicy == ContentCheckNotEvaluatedPolicy.FailClosed)
                    {
                        entries.Add(new ContentCheckPublishGateBlockingEntry
                        {
                            CheckAlias = rule.Check.Alias,
                            RequirementLevel = rule.RequirementLevel,
                            Message = "No check result exists for this content's current version — publish is blocked until this check has run against it.",
                        });
                    }

                    continue;
                }

                if (IsCountedAsFailing(fresh))
                {
                    entries.Add(new ContentCheckPublishGateBlockingEntry
                    {
                        CheckAlias = fresh.CheckAlias,
                        RequirementLevel = fresh.RequirementLevel,
                        Message = fresh.Result.Message,
                    });
                }
            }
        }

        return entries;
    }

    /// <summary>
    /// An entry counts as failing if it's a real <see cref="ContentCheckStatus.Fail"/>, or it's
    /// <see cref="ContentCheckStatus.NotEvaluated"/> with its rule set to
    /// <see cref="ContentCheckNotEvaluatedPolicy.FailClosed"/> — treated at the rule's own
    /// <see cref="ContentCheckStoredResult.RequirementLevel"/>, never a fixed level. See
    /// docs/guard-spec.md's "NotEvaluatedPolicy decides whether a missed check counts as failing
    /// at all."
    /// </summary>
    private static bool IsCountedAsFailing(ContentCheckStoredResult result) =>
        result.Result.Status == ContentCheckStatus.Fail
        || (result.Result.Status == ContentCheckStatus.NotEvaluated
            && result.NotEvaluatedPolicy == ContentCheckNotEvaluatedPolicy.FailClosed);

    /// <summary>
    /// A stable, order-sensitive key for a location path — <see cref="ContentCheckLocationStep"/>
    /// is a plain class with no structural equality of its own.
    /// </summary>
    private static string PathKey(IReadOnlyList<ContentCheckLocationStep> path) =>
        string.Join('/', path.Select(step => $"{step.PropertyAlias}:{step.ElementKey}"));
}
