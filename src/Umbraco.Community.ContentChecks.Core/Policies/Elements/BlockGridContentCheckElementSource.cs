using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Cache.PropertyEditors;
using Umbraco.Cms.Core.Models.Blocks;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;

namespace Umbraco.Community.ContentChecks.Core.Policies.Elements;

/// <summary>
/// Discovers element instances inside a Block Grid property. See
/// <see cref="BlockListContentCheckElementSource"/> for why this is built on Umbraco's own
/// block-editor parsing rather than a hand-rolled JSON walk. A block's per-area allow-list
/// (<c>BlockGridAreaConfiguration</c>) only ever narrows a subset of the types already declared on
/// the property's top-level <c>Blocks</c> array, so reading allowed types from that array alone is
/// complete — areas don't introduce element types the property-level configuration doesn't already
/// know about.
/// </summary>
[ContentCheckElementSource(Constants.PropertyEditors.Aliases.BlockGrid)]
public sealed class BlockGridContentCheckElementSource : ContentCheckElementSourceBase
{
    private readonly IJsonSerializer _jsonSerializer;
    private readonly IBlockEditorElementTypeCache _elementTypeCache;
    private readonly ILogger<BlockGridContentCheckElementSource> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlockGridContentCheckElementSource"/> class.
    /// </summary>
    /// <param name="jsonSerializer">The JSON serializer used to deserialize the property's raw value.</param>
    /// <param name="elementTypeCache">Resolves block content type keys to their <c>IContentType</c>.</param>
    /// <param name="logger">Logger passed through to Umbraco's own block-editor parsing.</param>
    public BlockGridContentCheckElementSource(
        IJsonSerializer jsonSerializer,
        IBlockEditorElementTypeCache elementTypeCache,
        ILogger<BlockGridContentCheckElementSource> logger)
    {
        _jsonSerializer = jsonSerializer;
        _elementTypeCache = elementTypeCache;
        _logger = logger;
    }

    /// <inheritdoc />
    public override IEnumerable<Guid> GetAllowedElementTypeKeys(object? dataTypeConfiguration)
    {
        var configuration = ConfigurationEditor.ConfigurationAs<BlockGridConfiguration>(dataTypeConfiguration);
        return BlockConfigurationHelper.GetAllowedElementTypeKeys(configuration?.Blocks);
    }

    /// <inheritdoc />
    public override IEnumerable<ContentCheckElementInstance> GetElementInstances(object? rawValue)
    {
        var values = new BlockEditorValues<BlockGridValue, BlockGridLayoutItem>(
            new BlockGridEditorDataConverter(_jsonSerializer), _elementTypeCache, _logger);

        var data = values.DeserializeAndClean(rawValue);
        if (data is null)
        {
            yield break;
        }

        foreach (var block in data.BlockValue.ContentData.Concat(data.BlockValue.SettingsData))
        {
            yield return ContentCheckElementInstanceMapper.FromBlockItemData(block);
        }
    }
}
