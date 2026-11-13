using System.Text.Json;
using NUnit.Framework;
using Umbraco.Community.ContentChecks.Core.Checks;
using Umbraco.Community.ContentChecks.Core.Config;
using Umbraco.Community.ContentChecks.Core.Markup;
using Umbraco.Community.ContentChecks.Core.Markup.Checks;
using Umbraco.Community.ContentChecks.Tests.Unit.Markup;

namespace Umbraco.Community.ContentChecks.Tests.Unit.Markup.Checks;

[TestFixture]
public class ImageAltTextCheckTests
{
    private ImageAltTextCheck _check = null!;

    [SetUp]
    public void SetUp() => _check = new ImageAltTextCheck(new FakeContentCheckInfrastructure());

    private static ContentCheckConfig ConfigFor(bool requireNonEmptyAltText = false)
        => new()
        {
            Config = JsonSerializer.SerializeToElement(
                new ImageAltTextCheckConfig { RequireNonEmptyAltText = requireNonEmptyAltText },
                JsonSerializerOptions.Web),
        };

    private static ContentCheckMarkupContext ContextFor(string html)
        => new()
        {
            Content = new InMemoryContentCheckSnapshot(Guid.NewGuid(), "page"),
            Markup = new ContentCheckMarkupResult { Success = true, Html = html },
            Document = MarkupTestHelpers.DocumentFor(html),
        };

    [Test]
    public async Task EvaluateAsync_NoImages_ReturnsPass()
    {
        var result = await _check.EvaluateAsync(
            ContextFor("<html><body><p>No images here.</p></body></html>"),
            ConfigFor(),
            CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ContentCheckStatus.Pass));
        Assert.That(result.Message, Does.Contain("no images"));
    }

    [Test]
    public async Task EvaluateAsync_EveryImageHasAltText_ReturnsPass()
    {
        var html = """<html><body><img src="a.jpg" alt="A cat"><img src="b.jpg" alt="A dog"></body></html>""";

        var result = await _check.EvaluateAsync(ContextFor(html), ConfigFor(), CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ContentCheckStatus.Pass));
    }

    [Test]
    public async Task EvaluateAsync_AnImageWithNoAltAttribute_ReturnsFail()
    {
        var html = """<html><body><img src="a.jpg" alt="A cat"><img src="missing.jpg"></body></html>""";

        var result = await _check.EvaluateAsync(ContextFor(html), ConfigFor(), CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ContentCheckStatus.Fail));
        Assert.That(result.Message, Does.Contain("1 of 2"));
        Assert.That(result.Message, Does.Contain("missing.jpg"));
    }

    [Test]
    public async Task EvaluateAsync_EmptyAltText_DefaultConfig_IsNotAFailure()
    {
        // alt="" is the standards-correct way to mark a decorative image -- the default,
        // lenient config must not flag it.
        var html = """<html><body><img src="decorative.jpg" alt=""></body></html>""";

        var result = await _check.EvaluateAsync(ContextFor(html), ConfigFor(requireNonEmptyAltText: false), CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ContentCheckStatus.Pass));
    }

    [Test]
    public async Task EvaluateAsync_EmptyAltText_StrictConfig_IsAFailure()
    {
        var html = """<html><body><img src="decorative.jpg" alt=""></body></html>""";

        var result = await _check.EvaluateAsync(ContextFor(html), ConfigFor(requireNonEmptyAltText: true), CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ContentCheckStatus.Fail));
    }

    [Test]
    public async Task EvaluateAsync_MoreThanFiveOffendingImages_TruncatesTheListedSources()
    {
        var images = string.Join(string.Empty, Enumerable.Range(1, 7).Select(i => $"""<img src="{i}.jpg">"""));
        var html = $"<html><body>{images}</body></html>";

        var result = await _check.EvaluateAsync(ContextFor(html), ConfigFor(), CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ContentCheckStatus.Fail));
        Assert.That(result.Message, Does.Contain("7 of 7"));
        Assert.That(result.Message, Does.Contain("2 more"));
    }

    [Test]
    public async Task EvaluateAsync_ReturnsAWholePageTarget_WithTheRequestedCultureAndSegment()
    {
        const string html = "<html><body></body></html>";
        var context = new ContentCheckMarkupContext
        {
            Content = new InMemoryContentCheckSnapshot(Guid.NewGuid(), "page"),
            Culture = "da-DK",
            Segment = "mobile",
            Markup = new ContentCheckMarkupResult { Success = true, Html = html },
            Document = MarkupTestHelpers.DocumentFor(html),
        };

        var result = await _check.EvaluateAsync(context, ConfigFor(), CancellationToken.None);

        var target = result.Targets!.Single();
        Assert.That(target.PropertyAlias, Is.Null);
        Assert.That(target.Culture, Is.EqualTo("da-DK"));
        Assert.That(target.Segment, Is.EqualTo("mobile"));
    }

    private sealed class FakeContentCheckInfrastructure : IContentCheckInfrastructure
    {
        public IContentCheckConfigSchemaBuilder SchemaBuilder { get; } = new ContentCheckConfigSchemaBuilder();

        public IContentCheckConfigResolver ConfigResolver { get; } = new ContentCheckConfigResolver();
    }
}
