namespace Umbraco.Community.ContentChecks.Core.Markup;

/// <summary>
/// The outcome of fetching a content item's rendered markup, per
/// docs/markup-checks-exploration.md §9.2. A provider failure (no route, fetch error, a non-2xx
/// response) maps onto <c>ContentCheckStatus.NotEvaluated</c>/<c>ContentCheckNotEvaluatedReason.Error</c>
/// wherever a markup check consumes this -- the same existing machinery every other check
/// failure already uses, not a new mechanism.
/// </summary>
public sealed class ContentCheckMarkupResult
{
    /// <summary>
    /// Whether the fetch succeeded and <see cref="Html"/> is usable.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// The fetched HTML, when <see cref="Success"/> is <c>true</c>.
    /// </summary>
    public string? Html { get; init; }

    /// <summary>
    /// The URL that was (or would have been) fetched, for diagnostics -- populated even on
    /// failure once a URL was resolved, so a check's error message can show it.
    /// </summary>
    public string? Url { get; init; }

    /// <summary>
    /// The HTTP status code returned, if a request was actually made.
    /// </summary>
    public int? StatusCode { get; init; }

    /// <summary>
    /// A human-readable reason, when <see cref="Success"/> is <c>false</c>.
    /// </summary>
    public string? FailureReason { get; init; }
}
