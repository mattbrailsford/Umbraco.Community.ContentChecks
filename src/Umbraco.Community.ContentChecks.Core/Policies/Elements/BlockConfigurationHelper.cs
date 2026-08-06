using Umbraco.Cms.Core.PropertyEditors;

namespace Umbraco.Community.ContentChecks.Core.Policies.Elements;

/// <summary>
/// Reads allowed element type keys from a set of block configurations — the shared shape behind
/// Block List's and Block Grid's own per-block configuration types, both of which implement
/// <see cref="IBlockConfiguration"/>. Block-specific, so it lives beside the two built-in Block
/// sources rather than on <see cref="ContentCheckElementSourceBase"/>, which stays usable by an
/// element source for any other property editor, not only Block-shaped ones.
/// </summary>
internal static class BlockConfigurationHelper
{
    /// <summary>
    /// Yields the content and settings element type keys declared on a set of block configurations.
    /// </summary>
    /// <param name="blocks">The configured blocks, or <c>null</c> if there are none.</param>
    /// <returns>The content and settings element type keys the blocks allow.</returns>
    public static IEnumerable<Guid> GetAllowedElementTypeKeys(IEnumerable<IBlockConfiguration>? blocks)
    {
        if (blocks is null)
        {
            yield break;
        }

        foreach (var block in blocks)
        {
            yield return block.ContentElementTypeKey;
            if (block.SettingsElementTypeKey is { } settingsElementTypeKey)
            {
                yield return settingsElementTypeKey;
            }
        }
    }
}
