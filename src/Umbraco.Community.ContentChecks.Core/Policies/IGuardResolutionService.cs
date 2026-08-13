using Umbraco.Cms.Core.Models;
using Umbraco.Community.ContentChecks.Core.Checks;

namespace Umbraco.Community.ContentChecks.Core.Policies;

/// <summary>
/// Resolves what a failing rule actually does — the Guard half of
/// docs/guard-spec.md, kept separate from <see cref="IContentCheckResolutionService"/>
/// (which resolves policies/rules, not enforcement).
/// </summary>
public interface IGuardResolutionService
{
    /// <summary>
    /// Resolves the <see cref="ContentCheckResponse"/> a rule at <paramref name="requirementLevel"/>
    /// produces for <paramref name="content"/>, for the given user. No matching guard — or no
    /// guard configured anywhere above the content — always resolves to
    /// <see cref="ContentCheckResponse.Warn"/>, never <c>Block</c>: enforcement is opt-in per
    /// section, not a default to code around.
    /// </summary>
    /// <param name="content">The content being evaluated.</param>
    /// <param name="requirementLevel">The failing rule's requirement level.</param>
    /// <param name="currentUserKey">The user the response is being resolved for, or <c>null</c> if there isn't one (e.g. a background run).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The resolved response.</returns>
    Task<ContentCheckResponse> ResolveResponseAsync(
        IContent content,
        RequirementLevel requirementLevel,
        Guid? currentUserKey,
        CancellationToken cancellationToken = default);
}
