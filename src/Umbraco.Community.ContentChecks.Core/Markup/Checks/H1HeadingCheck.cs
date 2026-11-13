using Umbraco.Community.ContentChecks.Core.Checks;
using Umbraco.Community.ContentChecks.Core.Config;

namespace Umbraco.Community.ContentChecks.Core.Markup.Checks;

/// <summary>
/// Configuration for <see cref="H1HeadingCheck"/>.
/// </summary>
public class H1HeadingCheckConfig
{
    /// <summary>
    /// Whether more than one <c>&lt;h1&gt;</c> on the page also counts as a failure.
    /// </summary>
    /// <remarks>
    /// Off by default -- a page with more than one <c>&lt;h1&gt;</c> contradicts the standard
    /// "one primary heading per page" SEO/accessibility guidance, so it fails alongside having
    /// none at all. Turn this on for templates that intentionally render more than one (a
    /// card/listing layout, say) and only want this check to catch a page with none.
    /// </remarks>
    [ContentCheckField(
        Label = "Allow multiple H1 headings",
        Description = "When off, a page with more than one H1 heading fails this check, matching the standard one-H1-per-page guidance. Turn this on if your templates intentionally render more than one and you only want to catch a page with none at all.",
        SortOrder = 1)]
    public bool AllowMultipleH1 { get; set; }
}

/// <summary>
/// An <see cref="IMarkupContentCheck"/> (<c>Metered</c> + <c>Deterministic</c>): fails when the
/// rendered page doesn't have exactly one <c>&lt;h1&gt;</c> heading.
/// </summary>
/// <remarks>
/// Always <c>Metered</c>, never <c>Free</c>, even though the HTML parsing itself is cheap -- a
/// markup check is paired with a page fetch over the network, which never is (see
/// <see cref="IMarkupContentCheck"/>'s own remarks).
/// </remarks>
[ContentCheck("h1-heading", "H1 heading", Cost = ContentCheckCost.Metered, Kind = ContentCheckKind.Deterministic)]
public sealed class H1HeadingCheck : MarkupContentCheckBase<H1HeadingCheckConfig>
{
    /// <inheritdoc />
    public override string Description => "Fails when the rendered page doesn't have exactly one H1 heading.";

    /// <summary>
    /// Initializes a new instance of the <see cref="H1HeadingCheck"/> class.
    /// </summary>
    /// <param name="infrastructure">The check infrastructure dependencies.</param>
    public H1HeadingCheck(IContentCheckInfrastructure infrastructure)
        : base(infrastructure)
    {
    }

    /// <inheritdoc />
    protected override async Task<ContentCheckResult> EvaluateAsync(
        ContentCheckMarkupContext context,
        H1HeadingCheckConfig config,
        CancellationToken cancellationToken)
    {
        var target = new ContentCheckTarget { Culture = context.Culture, Segment = context.Segment };

        // Shared across every markup check in this run -- see ContentCheckMarkupContext.Document's
        // own remarks. Parses on first access only; a second structural markup check in the same
        // run reuses this exact instance instead of re-parsing the same HTML.
        var document = await context.GetDocumentAsync();

        var headings = document.DocumentNode.SelectNodes("//h1");
        var count = headings?.Count ?? 0;

        if (count == 0)
        {
            return new ContentCheckResult
            {
                Status = ContentCheckStatus.Fail,
                Message = "The page has no H1 heading.",
                Targets = [target],
            };
        }

        if (count == 1)
        {
            return new ContentCheckResult
            {
                Status = ContentCheckStatus.Pass,
                Message = "The page has exactly one H1 heading.",
                Targets = [target],
            };
        }

        if (config.AllowMultipleH1)
        {
            return new ContentCheckResult
            {
                Status = ContentCheckStatus.Pass,
                Message = $"The page has {count} H1 headings.",
                Targets = [target],
            };
        }

        const int maxListed = 5;
        var texts = headings!.Select(h => h.InnerText.Trim()).Where(t => t.Length > 0).ToList();
        var listed = string.Join(", ", texts.Take(maxListed).Select(t => $"\"{t}\""));
        var suffix = texts.Count > maxListed ? $", and {texts.Count - maxListed} more" : string.Empty;

        return new ContentCheckResult
        {
            Status = ContentCheckStatus.Fail,
            Message = $"The page has {count} H1 headings, expected exactly one: {listed}{suffix}.",
            Targets = [target],
        };
    }
}
