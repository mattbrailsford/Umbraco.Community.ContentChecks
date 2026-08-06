using Umbraco.Cms.Core.Composing;

namespace Umbraco.Community.ContentChecks.Core.Checks;

/// <summary>
/// A lazy collection builder for check types.
/// </summary>
/// <remarks>
/// Check types are auto-discovered via <see cref="IDiscoverable"/> and the
/// <see cref="ContentCheckAttribute"/> — not <c>OrderedCollectionBuilder</c>, which is for
/// middleware pipelines, a different shape of problem. Use
/// <see cref="LazyCollectionBuilderBase{TBuilder,TCollection,TItem}.Add{T}"/> to add checks
/// manually, or <see cref="LazyCollectionBuilderBase{TBuilder,TCollection,TItem}.Exclude{T}"/>
/// to exclude auto-discovered ones.
/// </remarks>
public class ContentCheckCollectionBuilder
    : LazyCollectionBuilderBase<ContentCheckCollectionBuilder, ContentCheckCollection, IContentCheck>
{
    /// <inheritdoc />
    protected override ContentCheckCollectionBuilder This => this;
}
