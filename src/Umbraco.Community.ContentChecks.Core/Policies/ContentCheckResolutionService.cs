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

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentCheckResolutionService"/> class.
    /// </summary>
    /// <param name="policyService">Looks up policies by content type key.</param>
    /// <param name="checks">The registered check types, to resolve a rule's check alias.</param>
    /// <param name="elementSources">The registered element sources, for cascading.</param>
    /// <param name="contentTypeService">Resolves content types and their composition graph.</param>
    /// <param name="entityService">Resolves a start node key to its path id.</param>
    public ContentCheckResolutionService(
        IContentCheckPolicyService policyService,
        ContentCheckCollection checks,
        ContentCheckElementSourceCollection elementSources,
        IContentTypeService contentTypeService,
        IEntityService entityService)
    {
        _policyService = policyService;
        _checks = checks;
        _elementSources = elementSources;
        _contentTypeService = contentTypeService;
        _entityService = entityService;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ContentCheckResolvedTarget>> ResolveAsync(
        IContent content,
        string? culture = null,
        string? segment = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<ContentCheckResolvedTarget>();

        var rootContentType = await _contentTypeService.GetAsync(content.ContentType.Key);
        if (rootContentType is null)
        {
            return results;
        }

        var rootPolicies = await GetMatchingPoliciesAsync(rootContentType.Key, rootContentType.CompositionKeys(), content, cancellationToken);
        var rootRules = ResolveRules(rootPolicies);
        if (rootRules.Count > 0)
        {
            results.Add(new ContentCheckResolvedTarget
            {
                Snapshot = new ContentCheckSnapshot(content),
                Rules = rootRules,
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

        await CascadeAsync(rootProperties, content, results, cancellationToken);

        return results;
    }

    private async Task CascadeAsync(
        IReadOnlyList<(string Alias, string? PropertyEditorAlias, object? RawValue)> properties,
        IContent rootContent,
        List<ContentCheckResolvedTarget> results,
        CancellationToken cancellationToken)
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
                var elementType = await _contentTypeService.GetAsync(instance.ContentTypeKey);
                if (elementType is not null)
                {
                    var matchingPolicies = await GetMatchingPoliciesAsync(
                        elementType.Key, elementType.CompositionKeys(), rootContent, cancellationToken);
                    var rules = ResolveRules(matchingPolicies);
                    if (rules.Count > 0)
                    {
                        results.Add(new ContentCheckResolvedTarget
                        {
                            Snapshot = new BlockElementContentCheckSnapshot(instance, rootContent.VersionId),
                            Rules = rules,
                            ContainingPropertyAlias = property.Alias,
                        });
                    }
                }

                var nestedProperties = instance.Values
                    .Select(v => (v.Alias, v.PropertyEditorAlias, (object?)v.Value))
                    .ToList();
                await CascadeAsync(nestedProperties, rootContent, results, cancellationToken);
            }
        }
    }

    private async Task<IReadOnlyList<ContentCheckPolicy>> GetMatchingPoliciesAsync(
        Guid contentTypeKey, IEnumerable<Guid> compositionKeys, IContent rootContent, CancellationToken cancellationToken)
    {
        var candidateKeys = new HashSet<Guid>(compositionKeys) { contentTypeKey };
        var policies = await _policyService.GetPoliciesByContentTypeKeysAsync(candidateKeys, cancellationToken);
        return policies.Where(p => IsWithinStartNode(rootContent, p.StartNodeKey)).ToList();
    }

    private bool IsWithinStartNode(IContent content, Guid? startNodeKey)
    {
        if (startNodeKey is null)
        {
            return true;
        }

        var startNodeId = _entityService.Get(startNodeKey.Value)?.Id;
        if (startNodeId is null)
        {
            return false;
        }

        return content.Path.Split(',').Contains(startNodeId.Value.ToString());
    }

    private List<ResolvedContentCheckRule> ResolveRules(IEnumerable<ContentCheckPolicy> policies)
    {
        var rules = new List<ResolvedContentCheckRule>();
        foreach (var policy in policies)
        {
            foreach (var rule in policy.Rules)
            {
                var check = _checks.GetByAlias(rule.CheckAlias);
                if (check is null)
                {
                    continue;
                }

                rules.Add(new ResolvedContentCheckRule
                {
                    Check = check,
                    Config = rule.Config,
                    Mode = rule.Mode,
                });
            }
        }

        return rules;
    }
}
