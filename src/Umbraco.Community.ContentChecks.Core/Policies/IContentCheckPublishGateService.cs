using Umbraco.Cms.Core.Models;

namespace Umbraco.Community.ContentChecks.Core.Policies;

/// <summary>
/// Resolves whether a content item's most recently stored check report blocks the current
/// publishing user — the still-unbuilt half of docs/guard-spec.md, called only from the
/// publish-gate notification handler. Never called from a run itself; see
/// "Runs are never user-specific" in that doc.
/// </summary>
public interface IContentCheckPublishGateService
{
    /// <summary>
    /// Evaluates the publish gate for <paramref name="content"/>, for the user about to publish it.
    /// </summary>
    /// <param name="content">The content item being published.</param>
    /// <param name="culture">The culture being published, or <c>null</c> if the content isn't variant.</param>
    /// <param name="segment">The segment being published, or <c>null</c> if the content isn't segmented.</param>
    /// <param name="currentUserKey">The user attempting to publish, or <c>null</c> if there isn't one (e.g. a background/scheduled publish).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The gate's decision, and every entry responsible for it.</returns>
    Task<ContentCheckPublishGateResult> EvaluateAsync(
        IContent content,
        string? culture,
        string? segment,
        Guid? currentUserKey,
        CancellationToken cancellationToken = default);
}
