using HtmlAgilityPack;
using Umbraco.Community.ContentChecks.Core.Checks;

namespace Umbraco.Community.ContentChecks.Core.Markup;

/// <summary>
/// What an <see cref="IMarkupContentCheck"/> evaluates: the already-fetched markup for one
/// content item, plus the variant being checked. Mirrors <see cref="ContentCheckContext"/>
/// (<see cref="Content"/>/<see cref="Culture"/>/<see cref="Segment"/>), with <see cref="Markup"/>
/// added to carry the fetch outcome.
/// </summary>
public sealed class ContentCheckMarkupContext
{
    /// <summary>
    /// The content the markup was fetched for. Always the root content item — a markup check
    /// never runs against a nested element instance (docs/markup-checks-exploration.md §5).
    /// </summary>
    public required IContentCheckSnapshot Content { get; init; }

    /// <summary>
    /// The culture being evaluated, or <c>null</c> for an invariant check.
    /// </summary>
    public string? Culture { get; init; }

    /// <summary>
    /// The segment being evaluated, or <c>null</c> for an unsegmented check.
    /// </summary>
    public string? Segment { get; init; }

    /// <summary>
    /// The fetch outcome. A check's base class (<see cref="MarkupContentCheckBase{TConfig}"/>)
    /// already short-circuits to <c>NotEvaluated</c> when this failed, so a check's own
    /// <c>EvaluateAsync</c> override can assume <see cref="ContentCheckMarkupResult.Success"/> is
    /// <c>true</c> and <see cref="ContentCheckMarkupResult.Html"/> is populated.
    /// </summary>
    public required ContentCheckMarkupResult Markup { get; init; }

    /// <summary>
    /// The parsed <see cref="Markup"/>, built at most once per check run and shared across every
    /// markup rule in it — the same "fetch once per run" principle
    /// <see cref="Checks.ContentCheckRunner"/> already applies to the raw HTTP fetch itself
    /// (docs/markup-checks-exploration.md §4.2), extended one level up so two structural checks
    /// in the same run don't each re-parse the same HTML. Only actually parses on first access
    /// (via <see cref="GetDocumentAsync"/>) — a check that never looks at page structure (a raw
    /// regex check, an AI-judged check wanting plain text) never pays the parse cost at all.
    /// </summary>
    public required Lazy<Task<HtmlDocument>> Document { get; init; }

    /// <summary>
    /// Gets the parsed document, parsing it on first access and reusing that same parse for
    /// every other markup check in the same run.
    /// </summary>
    /// <returns>The parsed document.</returns>
    public Task<HtmlDocument> GetDocumentAsync() => Document.Value;
}
