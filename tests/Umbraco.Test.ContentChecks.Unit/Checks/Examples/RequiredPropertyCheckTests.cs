using System.Text.Json;
using NUnit.Framework;
using Umbraco.Community.ContentChecks.Core.Checks;
using Umbraco.Community.ContentChecks.Core.Checks.Examples;
using Umbraco.Community.ContentChecks.Core.Config;

namespace Umbraco.Community.ContentChecks.Tests.Unit.Checks.Examples;

[TestFixture]
public class RequiredPropertyCheckTests
{
    private RequiredPropertyCheck _check = null!;

    [SetUp]
    public void SetUp()
    {
        var infrastructure = new FakeContentCheckInfrastructure();
        _check = new RequiredPropertyCheck(infrastructure);
    }

    private static ContentCheckConfig ConfigFor(string propertyAlias)
        => new()
        {
            Config = JsonSerializer.SerializeToElement(
                new RequiredPropertyCheckConfig { PropertyAlias = propertyAlias },
                JsonSerializerOptions.Web),
        };

    [Test]
    public async Task EvaluateAsync_PropertyDoesNotExistOnContentType_ReturnsNotApplicable()
    {
        var snapshot = new InMemoryContentCheckSnapshot(Guid.NewGuid(), "page");
        var context = new ContentCheckContext { Content = snapshot };

        var result = await _check.EvaluateAsync(context, ConfigFor("metaDescription"), CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ContentCheckStatus.NotApplicable));
    }

    [Test]
    public async Task EvaluateAsync_PropertyExistsButEmpty_ReturnsFail()
    {
        var snapshot = new InMemoryContentCheckSnapshot(Guid.NewGuid(), "page")
            .SetValue("metaDescription", string.Empty);
        var context = new ContentCheckContext { Content = snapshot };

        var result = await _check.EvaluateAsync(context, ConfigFor("metaDescription"), CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ContentCheckStatus.Fail));
        Assert.That(result.Targets, Has.Count.EqualTo(1));
        Assert.That(result.Targets![0].PropertyAlias, Is.EqualTo("metaDescription"));
    }

    [Test]
    public async Task EvaluateAsync_PropertyHasValue_ReturnsPass()
    {
        var snapshot = new InMemoryContentCheckSnapshot(Guid.NewGuid(), "page")
            .SetValue("metaDescription", "A real description.");
        var context = new ContentCheckContext { Content = snapshot };

        var result = await _check.EvaluateAsync(context, ConfigFor("metaDescription"), CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ContentCheckStatus.Pass));
    }

    private sealed class FakeContentCheckInfrastructure : IContentCheckInfrastructure
    {
        public IContentCheckConfigSchemaBuilder SchemaBuilder { get; } = new ContentCheckConfigSchemaBuilder();

        public IContentCheckConfigResolver ConfigResolver { get; } = new ContentCheckConfigResolver();
    }
}
