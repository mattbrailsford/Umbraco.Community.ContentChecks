using Umbraco.Cms.Core.Mapping;
using Umbraco.Community.ContentChecks.Core.Policies;
using Umbraco.Community.ContentChecks.Web.Api.Management.Folders.Models;

namespace Umbraco.Community.ContentChecks.Web.Api.Management.Folders.Mapping;

/// <summary>
/// Map definitions for policy folder models.
/// </summary>
public class FolderMapDefinition : IMapDefinition
{
    /// <inheritdoc />
    public void DefineMaps(IUmbracoMapper mapper)
    {
        mapper.Define<ContentCheckPolicyFolder, FolderResponseModel>((_, _) => new FolderResponseModel(), MapToResponse);
        mapper.Define<FolderRequestModel, ContentCheckPolicyFolder>(
            (_, _) => new ContentCheckPolicyFolder { Name = string.Empty },
            MapFromRequest);
    }

    // Umbraco.Code.MapAll
    private static void MapToResponse(ContentCheckPolicyFolder source, FolderResponseModel target, MapperContext context)
    {
        target.Key = source.Key;
        target.Name = source.Name;
        target.ParentFolderKey = source.ParentFolderKey;
    }

    // Umbraco.Code.MapAll -Key
    private static void MapFromRequest(FolderRequestModel source, ContentCheckPolicyFolder target, MapperContext context)
    {
        target.Name = source.Name;
        target.ParentFolderKey = source.ParentFolderKey;
    }
}
