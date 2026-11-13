namespace Umbraco.Community.ContentChecks.Core.Markup;

/// <summary>
/// Identifies the content whose rendered markup a markup check needs, per
/// docs/markup-checks-exploration.md §9.2. Deliberately not the
/// <c>IContentCheckSnapshot</c> property-bag shape property checks use -- a markup provider
/// doesn't read properties, it just needs enough identity to resolve a URL and fetch it.
/// </summary>
public sealed class ContentCheckMarkupRequest
{
    /// <summary>
    /// The content item's key.
    /// </summary>
    public required Guid ContentKey { get; init; }

    /// <summary>
    /// The culture to fetch, or <c>null</c> for an invariant site.
    /// </summary>
    public string? Culture { get; init; }

    /// <summary>
    /// The segment to fetch, or <c>null</c> for the default segment.
    /// </summary>
    public string? Segment { get; init; }
}
