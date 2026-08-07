using System.ComponentModel.DataAnnotations;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Community.ContentChecks.Core.Checks;
using Umbraco.Community.ContentChecks.Core.Config;
using Umbraco.Community.ContentChecks.Web.Api.Management.CheckTypes.Models;

namespace Umbraco.Community.ContentChecks.Web.Api.Management.CheckTypes.Mapping;

/// <summary>
/// Map definitions for check-type models. One-way only — <see cref="IContentCheck"/> and its
/// config schema are code-registered, not persisted; this endpoint only ever reads them.
/// </summary>
public class CheckTypesMapDefinition : IMapDefinition
{
    /// <inheritdoc />
    public void DefineMaps(IUmbracoMapper mapper)
    {
        mapper.Define<IContentCheck, CheckTypeResponseModel>((_, _) => new CheckTypeResponseModel(), MapToResponse);
        mapper.Define<ContentCheckConfigSchema, CheckConfigSchemaResponseModel>(
            (_, _) => new CheckConfigSchemaResponseModel(), MapSchemaToResponse);
        mapper.Define<ContentCheckField, CheckConfigFieldResponseModel>(
            (_, _) => new CheckConfigFieldResponseModel(), MapFieldToResponse);
    }

    // Umbraco.Code.MapAll
    private static void MapToResponse(IContentCheck source, CheckTypeResponseModel target, MapperContext context)
    {
        target.Alias = source.Alias;
        target.Name = source.Name;
        target.Description = source.Description;
        target.Cost = source.Cost;
        target.Kind = source.Kind;
        var schema = source.GetConfigSchema();
        target.ConfigSchema = schema is null ? null : context.Map<ContentCheckConfigSchema, CheckConfigSchemaResponseModel>(schema);
    }

    // Umbraco.Code.MapAll
    private static void MapSchemaToResponse(ContentCheckConfigSchema source, CheckConfigSchemaResponseModel target, MapperContext context)
    {
        target.Fields = context.MapEnumerable<ContentCheckField, CheckConfigFieldResponseModel>(source.Fields);
    }

    // Umbraco.Code.MapAll
    private static void MapFieldToResponse(ContentCheckField source, CheckConfigFieldResponseModel target, MapperContext context)
    {
        target.Key = source.Key;
        target.Label = source.Label;
        target.Description = source.Description;
        target.EditorUiAlias = source.EditorUiAlias;
        target.EditorConfig = source.EditorConfig;
        target.DefaultValue = source.DefaultValue;
        target.SortOrder = source.SortOrder;
        target.Group = source.Group;
        target.IsRequired = source.ValidationRules.Any(r => r is RequiredAttribute);
    }
}
