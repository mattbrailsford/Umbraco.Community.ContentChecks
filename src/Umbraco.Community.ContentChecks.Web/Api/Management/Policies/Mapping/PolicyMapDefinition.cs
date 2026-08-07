using Umbraco.Cms.Core.Mapping;
using Umbraco.Community.ContentChecks.Core.Policies;
using Umbraco.Community.ContentChecks.Web.Api.Management.Policies.Models;

namespace Umbraco.Community.ContentChecks.Web.Api.Management.Policies.Mapping;

/// <summary>
/// Map definitions for policy models.
/// </summary>
public class PolicyMapDefinition : IMapDefinition
{
    /// <inheritdoc />
    public void DefineMaps(IUmbracoMapper mapper)
    {
        mapper.Define<ContentCheckPolicy, PolicyResponseModel>((_, _) => new PolicyResponseModel(), MapToResponse);
        mapper.Define<ContentCheckPolicyRule, PolicyRuleModel>((_, _) => new PolicyRuleModel(), MapRuleToResponse);

        mapper.Define<PolicyRequestModel, ContentCheckPolicy>(
            (_, _) => new ContentCheckPolicy { Alias = string.Empty, Name = string.Empty, ContentTypeKey = Guid.Empty },
            MapFromRequest);
        mapper.Define<PolicyRuleModel, ContentCheckPolicyRule>(
            (_, _) => new ContentCheckPolicyRule { CheckAlias = string.Empty },
            MapRuleFromRequest);
    }

    // Umbraco.Code.MapAll
    private static void MapToResponse(ContentCheckPolicy source, PolicyResponseModel target, MapperContext context)
    {
        target.Key = source.Key;
        target.Alias = source.Alias;
        target.Name = source.Name;
        target.ContentTypeKey = source.ContentTypeKey;
        target.StartNodeKey = source.StartNodeKey;
        target.ParentFolderKey = source.ParentFolderKey;
        target.Rules = context.MapEnumerable<ContentCheckPolicyRule, PolicyRuleModel>(source.Rules);
    }

    // Umbraco.Code.MapAll
    private static void MapRuleToResponse(ContentCheckPolicyRule source, PolicyRuleModel target, MapperContext context)
    {
        target.CheckAlias = source.CheckAlias;
        target.Config = source.Config.Config;
        target.Mode = source.Mode;
        target.NotEvaluatedPolicy = source.NotEvaluatedPolicy;
    }

    // Umbraco.Code.MapAll -Key
    private static void MapFromRequest(PolicyRequestModel source, ContentCheckPolicy target, MapperContext context)
    {
        target.Alias = source.Alias;
        target.Name = source.Name;
        target.ContentTypeKey = source.ContentTypeKey;
        target.StartNodeKey = source.StartNodeKey;
        target.ParentFolderKey = source.ParentFolderKey;
        target.Rules = context.MapEnumerable<PolicyRuleModel, ContentCheckPolicyRule>(source.Rules);
    }

    // Umbraco.Code.MapAll
    private static void MapRuleFromRequest(PolicyRuleModel source, ContentCheckPolicyRule target, MapperContext context)
    {
        target.CheckAlias = source.CheckAlias;
        target.Config = new() { Config = source.Config };
        target.Mode = source.Mode;
        target.NotEvaluatedPolicy = source.NotEvaluatedPolicy;
    }
}
