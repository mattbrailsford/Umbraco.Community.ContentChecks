using System.Text.Json;
using NUnit.Framework;
using Umbraco.Community.ContentChecks.Core.Checks;
using Umbraco.Community.ContentChecks.Core.Config;
using Umbraco.Community.ContentChecks.Core.Markup;
using Umbraco.Community.ContentChecks.Core.Markup.Checks;
using Umbraco.Community.ContentChecks.Tests.Unit.Markup;

namespace Umbraco.Community.ContentChecks.Tests.Unit.Markup.Checks;

[TestFixture]
public class H1HeadingCheckTests
{
    private H1HeadingCheck _check = null!;

    [SetUp]
    public void SetUp() => _check = new H1HeadingCheck(new FakeContentCheckInfrastructure());

    private static ContentCheckConfig ConfigFor(bool allowMultipleH1 = false)
        => new()
        {
            Config = JsonSerializer.SerializeToElement(
                new H1HeadingCheckConfig { AllowMultipleH1 = allowMultipleH1 },
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
    public async Task EvaluateAsync_ExactlyOneH1_ReturnsPass()
    {
        var html = """<html><body><h1>Welcome</h1><p>Some text.</p></body></html>""";

        var result = await _check.EvaluateAsync(ContextFor(html), ConfigFor(), CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ContentCheckStatus.Pass));
    }

    [Test]
    public async Task EvaluateAsync_NoH1_ReturnsFail()
    {
        var html = """<html><body><h2>Not the main heading</h2></body></html>""";

        var result = await _check.EvaluateAsync(ContextFor(html), ConfigFor(), CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ContentCheckStatus.Fail));
        Assert.That(result.Message, Does.Contain("no H1"));
    }

    [Test]
    public async Task EvaluateAsync_MultipleH1_DefaultConfig_ReturnsFail()
    {
        var html = """<html><body><h1>First</h1><h1>Second</h1></body></html>""";

        var result = await _check.EvaluateAsync(ContextFor(html), ConfigFor(), CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ContentCheckStatus.Fail));
        Assert.That(result.Message, Does.Contain("2 H1 headings"));
        Assert.That(result.Message, Does.Contain("First"));
        Assert.That(result.Message, Does.Contain("Second"));
    }

    [Test]
    public async Task EvaluateAsync_MultipleH1_AllowMultipleConfig_ReturnsPass()
    {
        var html = """<html><body><h1>First</h1><h1>Second</h1></body></html>""";

        var result = await _check.EvaluateAsync(ContextFor(html), ConfigFor(allowMultipleH1: true), CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ContentCheckStatus.Pass));
        Assert.That(result.Message, Does.Contain("2 H1 headings"));
    }

    [Test]
    public async Task EvaluateAsync_MoreThanFiveH1s_TruncatesTheListedHeadings()
    {
        var headings = string.Join(string.Empty, Enumerable.Range(1, 7).Select(i => $"<h1>Heading {i}</h1>"));
        var html = $"<html><body>{headings}</body></html>";

        var result = await _check.EvaluateAsync(ContextFor(html), ConfigFor(), CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ContentCheckStatus.Fail));
        Assert.That(result.Message, Does.Contain("7 H1 headings"));
        Assert.That(result.Message, Does.Contain("2 more"));
    }

    [Test]
    public async Task EvaluateAsync_ReturnsAWholePageTarget_WithTheRequestedCultureAndSegment()
    {
        const string html = "<html><body><h1>Title</h1></body></html>";
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
