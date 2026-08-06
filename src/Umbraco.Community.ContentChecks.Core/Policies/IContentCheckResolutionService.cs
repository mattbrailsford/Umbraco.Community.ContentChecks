using Umbraco.Cms.Core.Models;

namespace Umbraco.Community.ContentChecks.Core.Policies;

/// <summary>
/// Resolves which check policies govern a content item — its own direct and composition matches,
/// plus every nested element instance discovered by cascading through its element-holding
/// properties, to any depth.
/// </summary>
public interface IContentCheckResolutionService
{
    /// <summary>
    /// Resolves every target (the root item, plus each nested element found by cascading) that has
    /// at least one applicable rule.
    /// </summary>
    /// <param name="content">The content item to resolve policies for.</param>
    /// <param name="culture">The culture to read variant property values for.</param>
    /// <param name="segment">The segment to read segmented property values for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Every target with at least one applicable rule.</returns>
    Task<IReadOnlyList<ContentCheckResolvedTarget>> ResolveAsync(
        IContent content,
        string? culture = null,
        string? segment = null,
        CancellationToken cancellationToken = default);
}
