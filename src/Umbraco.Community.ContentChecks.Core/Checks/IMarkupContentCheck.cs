using Umbraco.Community.ContentChecks.Core.Markup;

namespace Umbraco.Community.ContentChecks.Core.Checks;

/// <summary>
/// A check that evaluates a content item's rendered output markup rather than its stored
/// property values — see <see cref="ContentCheckMarkupContext"/> and
/// docs/markup-checks-exploration.md. Whole-page only: never participates in cascading into a
/// nested element instance (docs/markup-checks-exploration.md §5) — the resolution service
/// filters a rule referencing this interface out of every cascading match, so it only ever runs
/// against the root content item.
/// </summary>
/// <remarks>
/// Should realistically always declare <c>Cost.Metered</c> on its <see cref="ContentCheckAttribute"/>
/// — a network fetch is never actually free — but nothing enforces this at the type level,
/// consistent with this project's avoidance of compile-time constraints that don't earn their
/// keep.
/// </remarks>
public interface IMarkupContentCheck : IContentCheck
{
    /// <summary>
    /// Evaluates the check against a content item's rendered markup.
    /// </summary>
    /// <param name="context">The fetched markup, plus the content it was fetched for and the culture/segment.</param>
    /// <param name="config">The check's resolved configuration for this rule.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The evaluation result.</returns>
    Task<ContentCheckResult> EvaluateAsync(
        ContentCheckMarkupContext context,
        ContentCheckConfig config,
        CancellationToken cancellationToken);
}
