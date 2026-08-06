using System.ComponentModel.DataAnnotations;
using NUnit.Framework;
using Umbraco.Community.ContentChecks.Core.Checks.Examples;
using Umbraco.Community.ContentChecks.Core.Config;

namespace Umbraco.Community.ContentChecks.Tests.Unit.Config;

[TestFixture]
public class ContentCheckConfigSchemaBuilderTests
{
    private ContentCheckConfigSchemaBuilder _builder = null!;

    [SetUp]
    public void SetUp() => _builder = new ContentCheckConfigSchemaBuilder();

    [Test]
    public void BuildForType_ReadsExplicitLabelAndDescription()
    {
        var schema = _builder.BuildForType<RequiredPropertyCheckConfig>("required-property");

        var field = schema.Fields.Single(f => f.PropertyName == nameof(RequiredPropertyCheckConfig.PropertyAlias));
        Assert.That(field.Label, Is.EqualTo("Property alias"));
        Assert.That(field.Key, Is.EqualTo("propertyAlias"));
    }

    [Test]
    public void BuildForType_NonNullableStringProperty_InfersRequiredValidation()
    {
        var schema = _builder.BuildForType<RequiredPropertyCheckConfig>("required-property");

        var field = schema.Fields.Single(f => f.PropertyName == nameof(RequiredPropertyCheckConfig.PropertyAlias));
        Assert.That(field.ValidationRules.OfType<RequiredAttribute>(), Is.Not.Empty);
    }

    [Test]
    public void BuildForType_ReadsDefaultValueFromPropertyInitializer()
    {
        var schema = _builder.BuildForType<StubRubricCheckConfig>("stub-rubric");

        var field = schema.Fields.Single(f => f.PropertyName == nameof(StubRubricCheckConfig.Threshold));
        Assert.That(field.DefaultValue, Is.EqualTo(0.7));
    }
}
