namespace Umbraco.Community.ContentChecks.Core.Markup;

/// <summary>
/// Fetches a content item's rendered markup for a markup check to evaluate, per
/// docs/markup-checks-exploration.md §9. A single, DI-replaceable service (§9.1) -- most sites
/// have exactly one rendering pipeline, so a site swaps the whole implementation out (e.g. for a
/// headless frontend) rather than this registering a discovered collection like check types do.
/// </summary>
public interface IContentCheckMarkupProvider
{
    /// <summary>
    /// Fetches the rendered markup for the given content.
    /// </summary>
    /// <param name="request">Identifies the content to fetch.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The fetch outcome -- never throws for an ordinary fetch failure, see <see cref="ContentCheckMarkupResult"/>.</returns>
    Task<ContentCheckMarkupResult> GetMarkupAsync(ContentCheckMarkupRequest request, CancellationToken cancellationToken);
}
