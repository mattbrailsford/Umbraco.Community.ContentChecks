using Umbraco.Cms.Core.Models;

namespace Umbraco.Community.ContentChecks.Core.Policies;

/// <summary>
/// The primary entry point for running every check that applies to a content item and getting
/// back one combined report — the separate, prominently-named front door over resolution and the
/// runner, the same role <c>IAIChatService</c> plays over Umbraco.AI's own profile CRUD.
/// </summary>
public interface IContentCheckEvaluationService
{
    /// <summary>
    /// Resolves every applicable target for <paramref name="content"/>, runs each one through
    /// <see cref="Checks.IContentCheckRunner"/>, and combines the results into one report.
    /// </summary>
    /// <param name="content">The content item to evaluate.</param>
    /// <param name="culture">The culture to read variant property values for.</param>
    /// <param name="segment">The segment to read segmented property values for.</param>
    /// <param name="progress">
    /// Optional progress callback, reported once per resolved target as it finishes. Free for any
    /// caller that doesn't need it — only the Management API's live-run endpoint passes one.
    /// </param>
    /// <param name="currentUserKey">
    /// The user each rule's response is resolved for, or <c>null</c> if there isn't one (e.g. a
    /// background run). See <see cref="IContentCheckResolutionService.ResolveAsync"/>.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The combined report.</returns>
    Task<ContentCheckAggregateReport> EvaluateAsync(
        IContent content,
        string? culture = null,
        string? segment = null,
        IProgress<ContentCheckEvaluationProgress>? progress = null,
        Guid? currentUserKey = null,
        CancellationToken cancellationToken = default);
}
