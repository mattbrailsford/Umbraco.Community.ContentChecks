using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Cache.PropertyEditors;
using Umbraco.Cms.Core.Models.Blocks;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;

namespace Umbraco.Community.ContentChecks.Core.Policies.Elements;

/// <summary>
/// Discovers element instances inside a Block List property, built entirely on Umbraco's own
/// block-editor parsing (<see cref="BlockEditorValues{TValue,TLayout}"/>), not a hand-rolled JSON
/// walk. Reads the raw editorial value, so this works against draft/unsaved content — the
/// published-content route (<c>BlockListPropertyValueConverter</c>) genuinely requires a
/// published/preview content context that unsaved editor state doesn't have.
/// </summary>
[ContentCheckElementSource(Constants.PropertyEditors.Aliases.BlockList)]
public sealed class BlockListContentCheckElementSource : ContentCheckElementSourceBase
{
    private readonly IJsonSerializer _jsonSerializer;
    private readonly IBlockEditorElementTypeCache _elementTypeCache;
    private readonly ILogger<BlockListContentCheckElementSource> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlockListContentCheckElementSource"/> class.
    /// </summary>
    /// <param name="jsonSerializer">The JSON serializer used to deserialize the property's raw value.</param>
    /// <param name="elementTypeCache">Resolves block content type keys to their <c>IContentType</c>.</param>
    /// <param name="logger">Logger passed through to Umbraco's own block-editor parsing.</param>
    public BlockListContentCheckElementSource(
        IJsonSerializer jsonSerializer,
        IBlockEditorElementTypeCache elementTypeCache,
        ILogger<BlockListContentCheckElementSource> logger)
    {
        _jsonSerializer = jsonSerializer;
        _elementTypeCache = elementTypeCache;
        _logger = logger;
    }

    /// <inheritdoc />
    public override IEnumerable<Guid> GetAllowedElementTypeKeys(object? dataTypeConfiguration)
    {
        var configuration = ConfigurationEditor.ConfigurationAs<BlockListConfiguration>(dataTypeConfiguration);
        return GetAllowedElementTypeKeys(configuration?.Blocks);
    }

    /// <inheritdoc />
    public override IEnumerable<ContentCheckElementInstance> GetElementInstances(object? rawValue)
    {
        var values = new BlockEditorValues<BlockListValue, BlockListLayoutItem>(
            new BlockListEditorDataConverter(_jsonSerializer), _elementTypeCache, _logger);

        var data = values.DeserializeAndClean(rawValue);
        if (data is null)
        {
            yield break;
        }

        // Settings blocks are element-type instances in their own right (they can carry their own
        // check policies) -- cascaded the same as content blocks, for consistency with what
        // Effective Checks (design time) already reports as allowed via SettingsElementTypeKey.
        foreach (var block in data.BlockValue.ContentData.Concat(data.BlockValue.SettingsData))
        {
            yield return ContentCheckElementInstanceMapper.FromBlockItemData(block);
        }
    }
}
