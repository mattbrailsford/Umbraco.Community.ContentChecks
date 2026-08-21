using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Core.Services;
using Umbraco.Community.ContentChecks.Core.Checks;
using Umbraco.Community.ContentChecks.Core.Policies;
using Umbraco.Community.ContentChecks.Web.Api.Management.ApplicableChecks.Models;

namespace Umbraco.Community.ContentChecks.Web.Api.Management.ApplicableChecks.Mapping;

/// <summary>
/// Map definitions for Applicable Checks models. One-way only — <see cref="ContentCheckResolvedTarget"/>
/// is resolution's own internal output; this endpoint only ever reads it.
/// </summary>
public class ApplicableChecksMapDefinition : IMapDefinition
{
    private readonly IContentTypeService _contentTypeService;

    public ApplicableChecksMapDefinition(IContentTypeService contentTypeService)
    {
        _contentTypeService = contentTypeService;
    }

    /// <inheritdoc />
    public void DefineMaps(IUmbracoMapper mapper)
    {
        mapper.Define<ContentCheckResolvedTarget, ApplicableCheckTargetResponseModel>(
            (_, _) => new ApplicableCheckTargetResponseModel(), MapToResponse);
        mapper.Define<ContentCheckResolvedPolicyMatch, ApplicablePolicyMatchResponseModel>(
            (_, _) => new ApplicablePolicyMatchResponseModel(), MapMatchToResponse);
        mapper.Define<ResolvedContentCheckRule, ApplicableRuleResponseModel>(
            (_, _) => new ApplicableRuleResponseModel(), MapRuleToResponse);
    }

    // Umbraco.Code.MapAll
    private static void MapToResponse(ContentCheckResolvedTarget source, ApplicableCheckTargetResponseModel target, MapperContext context)
    {
        target.ContentTypeAlias = source.Snapshot.ContentTypeAlias;
        target.Path = source.Path;
        target.PolicyMatches = context.MapEnumerable<ContentCheckResolvedPolicyMatch, ApplicablePolicyMatchResponseModel>(source.PolicyMatches);
    }

    // Umbraco.Code.MapAll
    private void MapMatchToResponse(ContentCheckResolvedPolicyMatch source, ApplicablePolicyMatchResponseModel target, MapperContext context)
    {
        target.PolicyKey = source.Policy.Key;
        target.PolicyAlias = source.Policy.Alias;
        target.PolicyName = source.Policy.Name;
        target.Provenance = source.Provenance;
        target.ContentTypeName = _contentTypeService.Get(source.Policy.ContentTypeKey)?.Name;
        target.Rules = context.MapEnumerable<ResolvedContentCheckRule, ApplicableRuleResponseModel>(source.Rules);
    }

    // Umbraco.Code.MapAll
    private static void MapRuleToResponse(ResolvedContentCheckRule source, ApplicableRuleResponseModel target, MapperContext context)
    {
        target.RuleKey = source.RuleKey;
        target.CheckAlias = source.Check.Alias;
        target.CheckName = source.Check.Name;
        target.RuleName = source.Name;
        target.RequirementLevel = source.RequirementLevel;
    }
}
