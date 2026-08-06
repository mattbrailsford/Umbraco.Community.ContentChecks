using Umbraco.Cms.Core.Composing;

namespace Umbraco.Community.ContentChecks.Core.Policies.Elements;

/// <summary>
/// Answers, for one property-editor alias, the two questions cascading needs: at design time,
/// which element types a property's configuration allows; at runtime, which actual element
/// instances a property's stored value contains. Ships built in for Block List and Block Grid;
/// a site (or a future Umbraco release) can register its own for any other property editor that
/// holds element-type content, keyed by <see cref="PropertyEditorAlias"/>.
/// </summary>
public interface IContentCheckElementSource : IDiscoverable
{
    /// <summary>
    /// The alias of the property editor this source understands, e.g. <c>Umbraco.BlockList</c>.
    /// </summary>
    string PropertyEditorAlias { get; }

    /// <summary>
    /// The element type keys a property using this editor is configured to allow — the design-time
    /// question, used to compute Effective Checks with no live content involved.
    /// </summary>
    /// <param name="dataTypeConfiguration">The property's data type's configuration object
    /// (<c>IDataType.ConfigurationObject</c>).</param>
    /// <returns>The element type keys the configuration allows.</returns>
    IEnumerable<Guid> GetAllowedElementTypeKeys(object? dataTypeConfiguration);

    /// <summary>
    /// The element instances actually present in a property's stored value — the runtime question,
    /// used to discover what to run checks against. Reads the raw, unpublished editorial value, so
    /// this works against draft/unsaved content, not only published content.
    /// </summary>
    /// <param name="rawValue">The property's raw stored value, as returned by
    /// <see cref="Checks.IContentCheckSnapshot.GetValue{TValue}"/>.</param>
    /// <returns>The element instances found in the value.</returns>
    IEnumerable<ContentCheckElementInstance> GetElementInstances(object? rawValue);
}
