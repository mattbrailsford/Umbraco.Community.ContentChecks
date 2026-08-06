using Umbraco.Cms.Core.Models.Blocks;

namespace Umbraco.Community.ContentChecks.Core.Policies.Elements;

/// <summary>
/// Maps a resolved <see cref="BlockItemData"/> (shared shape between Block List and Block Grid)
/// to a <see cref="ContentCheckElementInstance"/>.
/// </summary>
internal static class ContentCheckElementInstanceMapper
{
    /// <summary>
    /// Maps a resolved <see cref="BlockItemData"/> to a <see cref="ContentCheckElementInstance"/>.
    /// </summary>
    /// <param name="block">The resolved block item data.</param>
    /// <returns>The mapped instance.</returns>
    public static ContentCheckElementInstance FromBlockItemData(BlockItemData block) => new()
    {
        Key = block.Key,
        ContentTypeKey = block.ContentTypeKey,
        ContentTypeAlias = block.ContentTypeAlias,
        Values = block.Values.Select(v => new ContentCheckElementPropertyValue
        {
            Alias = v.Alias,
            Culture = v.Culture,
            Segment = v.Segment,
            Value = v.Value,
            PropertyEditorAlias = v.PropertyType?.PropertyEditorAlias,
        }).ToList(),
    };
}
