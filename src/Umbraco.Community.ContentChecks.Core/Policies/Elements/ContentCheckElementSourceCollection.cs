using Umbraco.Cms.Core.Composing;

namespace Umbraco.Community.ContentChecks.Core.Policies.Elements;

/// <summary>
/// The collection of registered <see cref="IContentCheckElementSource"/>s.
/// </summary>
public sealed class ContentCheckElementSourceCollection : BuilderCollectionBase<IContentCheckElementSource>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentCheckElementSourceCollection"/> class.
    /// </summary>
    /// <param name="items">The registered element sources.</param>
    public ContentCheckElementSourceCollection(Func<IEnumerable<IContentCheckElementSource>> items)
        : base(items)
    {
    }

    /// <summary>
    /// Finds the registered source for a property editor alias, or <c>null</c> if that editor
    /// doesn't hold element-type content (or no source has been registered for it).
    /// </summary>
    /// <param name="propertyEditorAlias">The property editor alias.</param>
    /// <returns>The registered source, or <c>null</c> if none is registered for this alias.</returns>
    public IContentCheckElementSource? FindByPropertyEditorAlias(string propertyEditorAlias)
        => this.FirstOrDefault(s => s.PropertyEditorAlias == propertyEditorAlias);
}
