using Umbraco.Cms.Core.Composing;

namespace Umbraco.Community.ContentChecks.Core.Policies.Elements;

/// <summary>
/// Declares the property editor alias an <see cref="IContentCheckElementSource"/> understands.
/// Registration mirrors check types: <see cref="IDiscoverable"/> auto-discovery plus this
/// attribute, not a hand-maintained list.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class ContentCheckElementSourceAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentCheckElementSourceAttribute"/> class.
    /// </summary>
    /// <param name="propertyEditorAlias">The property editor alias this source understands.</param>
    public ContentCheckElementSourceAttribute(string propertyEditorAlias)
    {
        PropertyEditorAlias = propertyEditorAlias;
    }

    /// <summary>
    /// The alias of the property editor this source understands.
    /// </summary>
    public string PropertyEditorAlias { get; }
}
