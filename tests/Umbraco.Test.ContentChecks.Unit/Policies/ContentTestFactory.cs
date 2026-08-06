using Moq;
using Umbraco.Cms.Core.Models;

namespace Umbraco.Community.ContentChecks.Tests.Unit.Policies;

/// <summary>
/// Builds mocked Umbraco CMS models for resolution/effective-checks tests, so each test only
/// states the handful of properties it actually cares about.
/// </summary>
internal static class ContentTestFactory
{
    public static Mock<IPropertyType> PropertyType(
        string alias,
        string editorAlias,
        Guid dataTypeKey = default,
        ContentVariation variations = ContentVariation.Nothing)
    {
        var mock = new Mock<IPropertyType>();
        mock.SetupGet(p => p.Alias).Returns(alias);
        mock.SetupGet(p => p.PropertyEditorAlias).Returns(editorAlias);
        mock.SetupGet(p => p.DataTypeKey).Returns(dataTypeKey);
        mock.SetupGet(p => p.Variations).Returns(variations);
        return mock;
    }

    public static Mock<IContentType> ContentType(
        Guid key,
        IEnumerable<Guid>? compositionKeys = null,
        IEnumerable<IPropertyType>? propertyTypes = null)
    {
        var mock = new Mock<IContentType>();
        mock.SetupGet(t => t.Key).Returns(key);
        mock.Setup(t => t.CompositionKeys()).Returns(compositionKeys ?? []);
        mock.SetupGet(t => t.CompositionPropertyTypes).Returns(propertyTypes ?? []);
        return mock;
    }

    public static Mock<IContent> Content(
        Guid contentTypeKey,
        string path = "-1,1",
        int versionId = 1,
        Guid? key = null,
        IEnumerable<IProperty>? properties = null)
    {
        var mock = new Mock<IContent>();
        var simpleType = new Mock<ISimpleContentType>();
        simpleType.SetupGet(t => t.Key).Returns(contentTypeKey);

        mock.SetupGet(c => c.ContentType).Returns(simpleType.Object);
        mock.SetupGet(c => c.Path).Returns(path);
        mock.SetupGet(c => c.VersionId).Returns(versionId);
        mock.SetupGet(c => c.Key).Returns(key ?? Guid.NewGuid());
        mock.SetupGet(c => c.Properties).Returns(new PropertyCollection(properties ?? []));
        return mock;
    }

    public static IProperty Property(IPropertyType propertyType) => new Property(propertyType);

    /// <summary>
    /// Stubs <see cref="IContent.GetValue{TValue}"/> for one alias/culture/segment. <c>GetValue</c>
    /// is a real interface member (not an extension method reading <c>Properties</c>), so a mocked
    /// <see cref="IContent"/> needs this stubbed explicitly for whatever value a test's resolution
    /// logic actually reads.
    /// </summary>
    public static void SetupGetValue(this Mock<IContent> content, string alias, string? rawValue, string? culture = null, string? segment = null)
        => content.Setup(c => c.GetValue<string>(alias, culture, segment, false)).Returns(rawValue);
}
