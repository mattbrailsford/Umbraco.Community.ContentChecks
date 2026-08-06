using System.Reflection;
using Umbraco.Cms.Core.PropertyEditors;

namespace Umbraco.Community.ContentChecks.Core.Policies.Elements;

/// <summary>
/// What you actually write an element source by inheriting — reads
/// <see cref="PropertyEditorAlias"/> from the <see cref="ContentCheckElementSourceAttribute"/> once,
/// the same declare-once-read-via-reflection shape checks use for their own attribute.
/// </summary>
public abstract class ContentCheckElementSourceBase : IContentCheckElementSource
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentCheckElementSourceBase"/> class.
    /// </summary>
    protected ContentCheckElementSourceBase()
    {
        var attribute = GetType().GetCustomAttribute<ContentCheckElementSourceAttribute>()
            ?? throw new InvalidOperationException(
                $"{GetType()} must be decorated with [{nameof(ContentCheckElementSourceAttribute)}].");
        PropertyEditorAlias = attribute.PropertyEditorAlias;
    }

    /// <inheritdoc />
    public string PropertyEditorAlias { get; }

    /// <inheritdoc />
    public abstract IEnumerable<Guid> GetAllowedElementTypeKeys(object? dataTypeConfiguration);

    /// <inheritdoc />
    public abstract IEnumerable<ContentCheckElementInstance> GetElementInstances(object? rawValue);

    /// <summary>
    /// Yields the content and settings element type keys declared on a set of block
    /// configurations — the shared shape behind Block List's and Block Grid's own per-block
    /// configuration types, both of which implement <see cref="IBlockConfiguration"/>.
    /// </summary>
    /// <param name="blocks">The configured blocks, or <c>null</c> if there are none.</param>
    /// <returns>The content and settings element type keys the blocks allow.</returns>
    protected static IEnumerable<Guid> GetAllowedElementTypeKeys(IEnumerable<IBlockConfiguration>? blocks)
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
