using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Community.ContentChecks.Core.Checks;
using Umbraco.Community.ContentChecks.Core.Policies.Elements;
using Umbraco.Extensions;

namespace Umbraco.Community.ContentChecks.Core.Policies;

/// <summary>
/// Default implementation of <see cref="IContentCheckResolutionService"/>. Three responsibilities:
/// direct match, composition match (transitive — <see cref="IContentTypeComposition.CompositionKeys"/>
/// already walks the graph), and cascading match through every element-holding property, to any
/// depth, via the registered <see cref="IContentCheckElementSource"/>s. A nested element's start-node
/// filter is always checked against the *root* item's own location — a nested element has no
/// location of its own.
/// </summary>
internal sealed class ContentCheckResolutionService : IContentCheckResolutionService
{
    private readonly IContentCheckPolicyService _policyService;
    private readonly ContentCheckCollection _checks;
    private readonly ContentCheckElementSourceCollection _elementSources;
    private readonly IContentTypeService _contentTypeService;
    private readonly IEntityService _entityService;
    private readonly IGuardResolutionService _guardResolutionService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentCheckResolutionService"/> class.
    /// </summary>
    /// <param name="policyService">Looks up policies by content type key.</param>
    /// <param name="checks">The registered check types, to resolve a rule's check alias.</param>
    /// <param name="elementSources">The registered element sources, for cascading.</param>
    /// <param name="contentTypeService">Resolves content types and their composition graph.</param>
    /// <param name="entityService">Resolves a start node key to its path id.</param>
    /// <param name="guardResolutionService">Resolves each rule's <see cref="Checks.ContentCheckResponse"/>.</param>
    public ContentCheckResolutionService(
        IContentCheckPolicyService policyService,
        ContentCheckCollection checks,
        ContentCheckElementSourceCollection elementSources,
        IContentTypeService contentTypeService,
        IEntityService entityService,
        IGuardResolutionService guardResolutionService)
    {
        _policyService = policyService;
        _checks = checks;
        _elementSources = elementSources;
        _contentTypeService = contentTypeService;
        _entityService = entityService;
        _guardResolutionService = guardResolutionService;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ContentCheckResolvedTarget>> ResolveAsync(
        IContent content,
        string? culture = null,
        string? segment = null,
        Guid? currentUserKey = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<ContentCheckResolvedTarget>();

        // Scoped to this one call: a page with twenty instances of the same element type (or a
        // policy revisiting the same content type via several nested paths) would otherwise repeat
        // the same content-type/policy/start-node lookups once per instance.
        var cache = new ResolutionCache();

        var rootContentType = await GetContentTypeAsync(content.ContentType.Key, cache);
        if (rootContentType is null)
        {
            return results;
        }

        var rootPolicies = await GetMatchingPoliciesAsync(rootContentType.Key, rootContentType.CompositionKeys(), content, cache, cancellationToken);
        var rootMatches = await BuildPolicyMatchesAsync(rootPolicies, rootContentType.Key, cascading: false, content, currentUserKey, cancellationToken);
        if (rootMatches.Count > 0)
        {
            results.Add(new ContentCheckResolvedTarget
            {
                Snapshot = new ContentCheckSnapshot(content),
                PolicyMatches = rootMatches,
                Path = [],
            });
        }

        var rootProperties = content.Properties
            .Select(p => (
                Alias: p.Alias,
                PropertyEditorAlias: (string?)p.PropertyType.PropertyEditorAlias,
                RawValue: (object?)content.GetValue<string>(
                    p.Alias,
                    p.PropertyType.VariesByCulture() ? culture : null,
                    p.PropertyType.VariesBySegment() ? segment : null)))
            .ToList();

        await CascadeAsync(rootProperties, content, results, cache, currentUserKey, cancellationToken, path: []);

        return results;
    }

    private async Task CascadeAsync(
        IReadOnlyList<(string Alias, string? PropertyEditorAlias, object? RawValue)> properties,
        IContent rootContent,
        List<ContentCheckResolvedTarget> results,
        ResolutionCache cache,
        Guid? currentUserKey,
        CancellationToken cancellationToken,
        IReadOnlyList<ContentCheckLocationStep> path)
    {
        foreach (var property in properties)
        {
            if (property.PropertyEditorAlias is null)
            {
                continue;
            }

            var source = _elementSources.FindByPropertyEditorAlias(property.PropertyEditorAlias);
            if (source is null)
            {
                continue;
            }

            foreach (var instance in source.GetElementInstances(property.RawValue))
            {
                var stepPath = new List<ContentCheckLocationStep>(path)
                {
                    new() { PropertyAlias = property.Alias, ElementKey = instance.Key },
                };

                var elementType = await GetContentTypeAsync(instance.ContentTypeKey, cache);
                if (elementType is not null)
                {
                    var matchingPolicies = await GetMatchingPoliciesAsync(
                        elementType.Key, elementType.CompositionKeys(), rootContent, cache, cancellationToken);
                    var matches = await BuildPolicyMatchesAsync(matchingPolicies, elementType.Key, cascading: true, rootContent, currentUserKey, cancellationToken);
                    if (matches.Count > 0)
                    {
                        results.Add(new ContentCheckResolvedTarget
                        {
                            Snapshot = new ContentCheckElementInstanceSnapshot(instance, rootContent.VersionId),
                            PolicyMatches = matches,
                            Path = stepPath,
                        });
                    }
                }

                var nestedProperties = instance.Values
                    .Select(v => (v.Alias, v.PropertyEditorAlias, (object?)v.Value))
                    .ToList();
                await CascadeAsync(nestedProperties, rootContent, results, cache, currentUserKey, cancellationToken, stepPath);
            }
        }
    }

    private async Task<IContentType?> GetContentTypeAsync(Guid contentTypeKey, ResolutionCache cache)
    {
        if (cache.ContentTypes.TryGetValue(contentTypeKey, out var cached))
        {
            return cached;
        }

        var contentType = await _contentTypeService.GetAsync(contentTypeKey);
        cache.ContentTypes[contentTypeKey] = contentType;
        return contentType;
    }

    private async Task<IReadOnlyList<ContentCheckPolicy>> GetMatchingPoliciesAsync(
        Guid contentTypeKey, IEnumerable<Guid> compositionKeys, IContent rootContent, ResolutionCache cache, CancellationToken cancellationToken)
    {
        if (!cache.PoliciesByContentTypeKey.TryGetValue(contentTypeKey, out var policies))
        {
            var candidateKeys = new HashSet<Guid>(compositionKeys) { contentTypeKey };
            policies = await _policyService.GetPoliciesByContentTypeKeysAsync(candidateKeys, cancellationToken);
            cache.PoliciesByContentTypeKey[contentTypeKey] = policies;
        }

        return policies.Where(p => IsWithinStartNode(rootContent, p.StartNodeKey, cache)).ToList();
    }

    private bool IsWithinStartNode(IContent content, Guid? startNodeKey, ResolutionCache cache)
    {
        if (startNodeKey is null)
        {
            return true;
        }

        if (!cache.StartNodeIds.TryGetValue(startNodeKey.Value, out var startNodeId))
        {
            startNodeId = _entityService.Get(startNodeKey.Value)?.Id;
            cache.StartNodeIds[startNodeKey.Value] = startNodeId;
        }

        if (startNodeId is null)
        {
            return false;
        }

        return content.Path.Split(',').Contains(startNodeId.Value.ToString());
    }

    /// <summary>
    /// Groups each matching policy with its own provenance and resolved rules, dropping any
    /// policy that has no rules resolving to a registered check. Cascading matches are always
    /// labeled <see cref="ContentCheckEffectiveCheckProvenance.Cascading"/> regardless of whether
    /// the policy targets the element type directly or via its own composition — once nested,
    /// that inner distinction doesn't matter to the outer view (mirrors
    /// <see cref="ContentCheckEffectiveChecksService"/>'s identical choice for the same reason).
    /// </summary>
    private async Task<List<ContentCheckResolvedPolicyMatch>> BuildPolicyMatchesAsync(
        IEnumerable<ContentCheckPolicy> policies,
        Guid contentTypeKey,
        bool cascading,
        IContent content,
        Guid? currentUserKey,
        CancellationToken cancellationToken)
    {
        var matches = new List<ContentCheckResolvedPolicyMatch>();
        foreach (var policy in policies)
        {
            var rules = await ResolveRulesAsync(policy, content, currentUserKey, cancellationToken);
            if (rules.Count == 0)
            {
                continue;
            }

            var provenance = cascading
                ? ContentCheckEffectiveCheckProvenance.Cascading
                : policy.ContentTypeKey == contentTypeKey
                    ? ContentCheckEffectiveCheckProvenance.Direct
                    : ContentCheckEffectiveCheckProvenance.Composition;

            matches.Add(new ContentCheckResolvedPolicyMatch
            {
                Policy = policy,
                Provenance = provenance,
                Rules = rules,
            });
        }

        return matches;
    }

    private async Task<List<ResolvedContentCheckRule>> ResolveRulesAsync(
        ContentCheckPolicy policy, IContent content, Guid? currentUserKey, CancellationToken cancellationToken)
    {
        var rules = new List<ResolvedContentCheckRule>();
        foreach (var rule in policy.Rules)
        {
            var check = _checks.GetByAlias(rule.CheckAlias);
            if (check is null)
            {
                continue;
            }

            var response = await _guardResolutionService.ResolveResponseAsync(
                content, rule.RequirementLevel, currentUserKey, cancellationToken);

            rules.Add(new ResolvedContentCheckRule
            {
                Check = check,
                Config = rule.Config,
                RequirementLevel = rule.RequirementLevel,
                Response = response,
            });
        }

        return rules;
    }

    /// <summary>
    /// Per-<see cref="ResolveAsync"/>-call memoization, so a page with many instances of the same
    /// element type (or a policy revisiting the same content type via several nested paths) pays
    /// for each content-type/policy/start-node lookup once, not once per instance.
    /// </summary>
    private sealed class ResolutionCache
    {
        public Dictionary<Guid, IContentType?> ContentTypes { get; } = [];

        public Dictionary<Guid, IReadOnlyList<ContentCheckPolicy>> PoliciesByContentTypeKey { get; } = [];

        public Dictionary<Guid, int?> StartNodeIds { get; } = [];
    }
}
