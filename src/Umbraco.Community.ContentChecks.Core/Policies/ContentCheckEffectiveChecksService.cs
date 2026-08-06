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

        var contentType = await _contentTypeService.GetAsync(contentTypeKey);
        if (contentType is null)
        {
            return results;
        }

        var candidateKeys = new HashSet<Guid>(contentType.CompositionKeys()) { contentType.Key };
        var directAndComposed = await _policyService.GetPoliciesByContentTypeKeysAsync(candidateKeys, cancellationToken);
        foreach (var policy in directAndComposed)
        {
            results.Add(policy.ContentTypeKey == contentType.Key
                ? new ContentCheckEffectiveCheck { Policy = policy, Provenance = ContentCheckEffectiveCheckProvenance.Direct }
                : new ContentCheckEffectiveCheck
                {
                    Policy = policy,
                    Provenance = ContentCheckEffectiveCheckProvenance.Composition,
                    ViaContentTypeKey = policy.ContentTypeKey,
                });
        }

        // Cycle guard is scoped to the current path (root -> ... -> here), not to every element
        // type visited anywhere in the walk, so the same element type reachable via two distinct
        // sibling properties is still reported for both -- only a genuine cycle back to an
        // ancestor already on this path is skipped.
        await WalkCascadingAsync(contentType, new HashSet<Guid> { contentType.Key }, results, cancellationToken);

        return results;
    }

    private async Task WalkCascadingAsync(
        IContentTypeComposition contentType,
        HashSet<Guid> visitedOnPath,
        List<ContentCheckEffectiveCheck> results,
        CancellationToken cancellationToken)
    {
        foreach (var propertyType in contentType.CompositionPropertyTypes)
        {
            var source = _elementSources.FindByPropertyEditorAlias(propertyType.PropertyEditorAlias);
            if (source is null)
            {
                continue;
            }

            var dataType = await _dataTypeService.GetAsync(propertyType.DataTypeKey);
            var allowedElementTypeKeys = source.GetAllowedElementTypeKeys(dataType?.ConfigurationObject).Distinct();

            foreach (var elementTypeKey in allowedElementTypeKeys)
            {
                if (visitedOnPath.Contains(elementTypeKey))
                {
                    continue;
                }

                var elementType = await _contentTypeService.GetAsync(elementTypeKey);
                if (elementType is null)
                {
                    continue;
                }

                var candidateKeys = new HashSet<Guid>(elementType.CompositionKeys()) { elementType.Key };
                var matches = await _policyService.GetPoliciesByContentTypeKeysAsync(candidateKeys, cancellationToken);
                foreach (var policy in matches)
                {
                    results.Add(new ContentCheckEffectiveCheck
                    {
                        Policy = policy,
                        Provenance = ContentCheckEffectiveCheckProvenance.Cascading,
                        ViaPropertyAlias = propertyType.Alias,
                        ViaElementTypeKey = elementType.Key,
                    });
                }

                var nextVisitedOnPath = new HashSet<Guid>(visitedOnPath) { elementTypeKey };
                await WalkCascadingAsync(elementType, nextVisitedOnPath, results, cancellationToken);
            }
        }
    }
}
