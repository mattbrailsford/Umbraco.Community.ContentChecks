using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Community.ContentChecks.Core.Policies.Elements;

namespace Umbraco.Community.ContentChecks.Core.Policies;

/// <summary>
/// Default implementation of <see cref="IContentCheckEffectiveChecksService"/>. Walks the type's
/// composition ancestry for direct/composition matches, and its element-holding properties'
/// allowed-element-type configuration, recursively, for cascading matches — schema-level, no live
/// content involved.
/// </summary>
internal sealed class ContentCheckEffectiveChecksService : IContentCheckEffectiveChecksService
{
    private readonly IContentCheckPolicyService _policyService;
    private readonly IContentTypeService _contentTypeService;
    private readonly IDataTypeService _dataTypeService;
    private readonly ContentCheckElementSourceCollection _elementSources;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentCheckEffectiveChecksService"/> class.
    /// </summary>
    /// <param name="policyService">Looks up policies by content type key.</param>
    /// <param name="contentTypeService">Resolves content types and their composition graph.</param>
    /// <param name="dataTypeService">Resolves a property's data type configuration.</param>
    /// <param name="elementSources">The registered element sources, for cascading.</param>
    public ContentCheckEffectiveChecksService(
        IContentCheckPolicyService policyService,
        IContentTypeService contentTypeService,
        IDataTypeService dataTypeService,
        ContentCheckElementSourceCollection elementSources)
    {
        _policyService = policyService;
        _contentTypeService = contentTypeService;
        _dataTypeService = dataTypeService;
        _elementSources = elementSources;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ContentCheckEffectiveCheck>> GetEffectiveChecksAsync(
        Guid contentTypeKey, CancellationToken cancellationToken = default)
    {
        var results = new List<ContentCheckEffectiveCheck>();

        // Scoped to this one call: the same element type reachable via several sibling properties
        // (a deliberate, non-deduplicated reporting case — see the cycle-guard note below) would
        // otherwise repeat the same content-type/policy/data-type lookups once per occurrence.
        var cache = new EffectiveChecksCache();

        var contentType = await GetContentTypeAsync(contentTypeKey, cache);
        if (contentType is null)
        {
            return results;
        }

        var directAndComposed = await GetMatchingPoliciesAsync(contentType.Key, contentType.CompositionKeys(), cache, cancellationToken);
        foreach (var policy in directAndComposed)
        {
            results.Add(policy.ContentTypeKey == contentType.Key
                ? new ContentCheckEffectiveCheck.Direct(policy)
                : new ContentCheckEffectiveCheck.Composition(policy, viaContentTypeKey: policy.ContentTypeKey));
        }

        // Cycle guard is scoped to the current path (root -> ... -> here), not to every element
        // type visited anywhere in the walk, so the same element type reachable via two distinct
        // sibling properties is still reported for both -- only a genuine cycle back to an
        // ancestor already on this path is skipped.
        await WalkCascadingAsync(contentType, new HashSet<Guid> { contentType.Key }, results, cache, cancellationToken);

        return results;
    }

    private async Task WalkCascadingAsync(
        IContentTypeComposition contentType,
        HashSet<Guid> visitedOnPath,
        List<ContentCheckEffectiveCheck> results,
        EffectiveChecksCache cache,
        CancellationToken cancellationToken)
    {
        foreach (var propertyType in contentType.CompositionPropertyTypes)
        {
            var source = _elementSources.FindByPropertyEditorAlias(propertyType.PropertyEditorAlias);
            if (source is null)
            {
                continue;
            }

            var dataType = await GetDataTypeAsync(propertyType.DataTypeKey, cache);
            var allowedElementTypeKeys = source.GetAllowedElementTypeKeys(dataType?.ConfigurationObject).Distinct();

            foreach (var elementTypeKey in allowedElementTypeKeys)
            {
                if (visitedOnPath.Contains(elementTypeKey))
                {
                    continue;
                }

                var elementType = await GetContentTypeAsync(elementTypeKey, cache);
                if (elementType is null)
                {
                    continue;
                }

                var matches = await GetMatchingPoliciesAsync(elementType.Key, elementType.CompositionKeys(), cache, cancellationToken);
                foreach (var policy in matches)
                {
                    results.Add(new ContentCheckEffectiveCheck.Cascading(policy, viaPropertyAlias: propertyType.Alias, viaElementTypeKey: elementType.Key));
                }

                var nextVisitedOnPath = new HashSet<Guid>(visitedOnPath) { elementTypeKey };
                await WalkCascadingAsync(elementType, nextVisitedOnPath, results, cache, cancellationToken);
            }
        }
    }

    private async Task<IContentType?> GetContentTypeAsync(Guid contentTypeKey, EffectiveChecksCache cache)
    {
        if (cache.ContentTypes.TryGetValue(contentTypeKey, out var cached))
        {
            return cached;
        }

        var contentType = await _contentTypeService.GetAsync(contentTypeKey);
        cache.ContentTypes[contentTypeKey] = contentType;
        return contentType;
    }

    private async Task<IDataType?> GetDataTypeAsync(Guid dataTypeKey, EffectiveChecksCache cache)
    {
        if (cache.DataTypes.TryGetValue(dataTypeKey, out var cached))
        {
            return cached;
        }

        var dataType = await _dataTypeService.GetAsync(dataTypeKey);
        cache.DataTypes[dataTypeKey] = dataType;
        return dataType;
    }

    private async Task<IReadOnlyList<ContentCheckPolicy>> GetMatchingPoliciesAsync(
        Guid contentTypeKey, IEnumerable<Guid> compositionKeys, EffectiveChecksCache cache, CancellationToken cancellationToken)
    {
        if (cache.PoliciesByContentTypeKey.TryGetValue(contentTypeKey, out var cached))
        {
            return cached;
        }

        var candidateKeys = new HashSet<Guid>(compositionKeys) { contentTypeKey };
        var policies = await _policyService.GetPoliciesByContentTypeKeysAsync(candidateKeys, cancellationToken);
        cache.PoliciesByContentTypeKey[contentTypeKey] = policies;
        return policies;
    }

    /// <summary>
    /// Per-<see cref="GetEffectiveChecksAsync"/>-call memoization, so a content type reached via
    /// several sibling properties pays for each content-type/policy/data-type lookup once, not
    /// once per occurrence.
    /// </summary>
    private sealed class EffectiveChecksCache
    {
        public Dictionary<Guid, IContentType?> ContentTypes { get; } = [];

        public Dictionary<Guid, IDataType?> DataTypes { get; } = [];

        public Dictionary<Guid, IReadOnlyList<ContentCheckPolicy>> PoliciesByContentTypeKey { get; } = [];
    }
}
