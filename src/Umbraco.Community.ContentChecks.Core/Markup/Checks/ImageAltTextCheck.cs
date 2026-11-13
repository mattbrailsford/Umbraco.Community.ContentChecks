using HtmlAgilityPack;
using Umbraco.Community.ContentChecks.Core.Checks;
using Umbraco.Community.ContentChecks.Core.Config;

namespace Umbraco.Community.ContentChecks.Core.Markup.Checks;

/// <summary>
/// Configuration for <see cref="ImageAltTextCheck"/>.
/// </summary>
public class ImageAltTextCheckConfig
{
    /// <summary>
    /// Whether an image with an empty <c>alt=""</c> also counts as a failure.
    /// </summary>
    /// <remarks>
    /// Off by default because <c>alt=""</c> is the standards-correct way to mark a purely
    /// decorative image (it tells a screen reader to skip it) -- only a genuinely missing
    /// <c>alt</c> attribute is a real accessibility defect on its own. Some teams want every
    /// image described for SEO reasons too, hence the opt-in.
    /// </remarks>
    [ContentCheckField(
        Label = "Require non-empty alt text",
        Description = "When off, an image with alt=\"\" counts as fine -- that's the correct way to mark a purely decorative image. Turn this on to also flag images with empty alt text.",
        SortOrder = 1)]
    public bool RequireNonEmptyAltText { get; set; }
}

/// <summary>
/// The first real <see cref="IMarkupContentCheck"/> (<c>Metered</c> + <c>Deterministic</c>):
/// fails when the rendered page has an <c>&lt;img&gt;</c> with no <c>alt</c> attribute at all.
/// Proves the markup contract end to end (docs/markup-checks-exploration.md) -- nothing else in
/// this repo implements <see cref="IMarkupContentCheck"/> yet.
/// </summary>
/// <remarks>
/// Always <c>Metered</c>, never <c>Free</c>, even though the HTML parsing itself is cheap -- a
/// markup check is paired with a page fetch over the network, which never is (see
/// <see cref="IMarkupContentCheck"/>'s own remarks).
/// </remarks>
[ContentCheck("image-alt-text", "Image alt text", Cost = ContentCheckCost.Metered, Kind = ContentCheckKind.Deterministic)]
public sealed class ImageAltTextCheck : MarkupContentCheckBase<ImageAltTextCheckConfig>
{
    /// <inheritdoc />
    public override string Description => "Fails when the rendered page has an image with no alt text.";

    /// <summary>
    /// Initializes a new instance of the <see cref="ImageAltTextCheck"/> class.
    /// </summary>
    /// <param name="infrastructure">The check infrastructure dependencies.</param>
    public ImageAltTextCheck(IContentCheckInfrastructure infrastructure)
        : base(infrastructure)
    {
    }

    /// <inheritdoc />
    protected override async Task<ContentCheckResult> EvaluateAsync(
        ContentCheckMarkupContext context,
        ImageAltTextCheckConfig config,
        CancellationToken cancellationToken)
    {
        var target = new ContentCheckTarget { Culture = context.Culture, Segment = context.Segment };

        // Shared across every markup check in this run -- see ContentCheckMarkupContext.Document's
        // own remarks. Parses on first access only; a second structural markup check in the same
        // run reuses this exact instance instead of re-parsing the same HTML.
        var document = await context.GetDocumentAsync();

        var images = document.DocumentNode.SelectNodes("//img");
        if (images is null)
        {
            return new ContentCheckResult
            {
                Status = ContentCheckStatus.Pass,
                Message = "The page has no images.",
                Targets = [target],
            };
        }

        var offendingSources = images
            .Where(img => IsOffending(img, config.RequireNonEmptyAltText))
            .Select(img => img.GetAttributeValue("src", "(no src)"))
            .ToList();

        if (offendingSources.Count == 0)
        {
            return new ContentCheckResult
            {
                Status = ContentCheckStatus.Pass,
                Message = $"All {images.Count} image(s) have alt text.",
                Targets = [target],
            };
        }

        const int maxListed = 5;
        var listed = string.Join(", ", offendingSources.Take(maxListed));
        var suffix = offendingSources.Count > maxListed ? $", and {offendingSources.Count - maxListed} more" : string.Empty;

        return new ContentCheckResult
        {
            Status = ContentCheckStatus.Fail,
            Message = $"{offendingSources.Count} of {images.Count} image(s) are missing alt text: {listed}{suffix}.",
            Targets = [target],
        };
    }

    private static bool IsOffending(HtmlNode image, bool requireNonEmptyAltText)
    {
        var altAttribute = image.Attributes["alt"];
        if (altAttribute is null)
        {
            return true;
        }

        return requireNonEmptyAltText && string.IsNullOrWhiteSpace(altAttribute.Value);
    }
}
