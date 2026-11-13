using HtmlAgilityPack;

namespace Umbraco.Community.ContentChecks.Tests.Unit.Markup;

/// <summary>
/// Shared test helper for building a <c>ContentCheckMarkupContext.Document</c>-shaped lazy
/// without going through a real <c>ContentCheckRunner</c> run.
/// </summary>
internal static class MarkupTestHelpers
{
    public static Lazy<Task<HtmlDocument>> DocumentFor(string html)
        => new(() =>
        {
            var document = new HtmlDocument();
            document.LoadHtml(html);
            return Task.FromResult(document);
        });
}
