using Umbraco.Cms.Core.Composing;

namespace Umbraco.Community.ContentChecks.Core.Policies.Elements;

/// <summary>
/// A lazy collection builder for element sources, auto-discovered via <see cref="IDiscoverable"/>
/// and <see cref="ContentCheckElementSourceAttribute"/> — the same shape as
/// <see cref="Checks.ContentCheckCollectionBuilder"/>, for the same reason.
/// </summary>
public class ContentCheckElementSourceCollectionBuilder
    : LazyCollectionBuilderBase<ContentCheckElementSourceCollectionBuilder, ContentCheckElementSourceCollection, IContentCheckElementSource>
{
    /// <inheritdoc />
    protected override ContentCheckElementSourceCollectionBuilder This => this;
}
