using Umbraco.Cms.Core.Mapping;
using Umbraco.Community.ContentChecks.Core.Policies;
using Umbraco.Community.ContentChecks.Web.Api.Management.Guards.Models;

namespace Umbraco.Community.ContentChecks.Web.Api.Management.Guards.Mapping;

/// <summary>
/// Map definitions for guard models.
/// </summary>
public class GuardMapDefinition : IMapDefinition
{
    /// <inheritdoc />
    public void DefineMaps(IUmbracoMapper mapper)
    {
        mapper.Define<Guard, GuardResponseModel>((_, _) => new GuardResponseModel(), MapToResponse);
        mapper.Define<GuardRule, GuardRuleModel>((_, _) => new GuardRuleModel(), MapRuleToResponse);

        mapper.Define<GuardRequestModel, Guard>(
            (_, _) => new Guard { Name = string.Empty },
            MapFromRequest);
        mapper.Define<GuardRuleModel, GuardRule>(
            (_, _) => new GuardRule(),
            MapRuleFromRequest);
    }

    // Umbraco.Code.MapAll
    private static void MapToResponse(Guard source, GuardResponseModel target, MapperContext context)
    {
        target.Key = source.Key;
        target.Name = source.Name;
        target.StartNodeKey = source.StartNodeKey;
        target.Rules = context.MapEnumerable<GuardRule, GuardRuleModel>(source.Rules);
    }

    // Umbraco.Code.MapAll
    private static void MapRuleToResponse(GuardRule source, GuardRuleModel target, MapperContext context)
    {
        target.Key = source.Key;
        target.UserKey = source.UserKey;
        target.RoleKey = source.RoleKey;
        target.BlockAt = source.BlockAt;
    }

    // Umbraco.Code.MapAll -Key
    private static void MapFromRequest(GuardRequestModel source, Guard target, MapperContext context)
    {
        target.Name = source.Name;
        target.StartNodeKey = source.StartNodeKey;
        target.Rules = context.MapEnumerable<GuardRuleModel, GuardRule>(source.Rules);
    }

    // Umbraco.Code.MapAll
    private static void MapRuleFromRequest(GuardRuleModel source, GuardRule target, MapperContext context)
    {
        // An empty key means the client is submitting a brand new rule -- generate its durable
        // identity here rather than trusting a client-supplied one, but preserve whatever key an
        // existing rule already carries.
        target.Key = source.Key == Guid.Empty ? Guid.NewGuid() : source.Key;
        target.UserKey = source.UserKey;
        target.RoleKey = source.RoleKey;
        target.BlockAt = source.BlockAt;
    }
}
