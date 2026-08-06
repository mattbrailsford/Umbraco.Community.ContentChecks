using Moq;
using NUnit.Framework;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Community.ContentChecks.Core.Policies;
using Umbraco.Community.ContentChecks.Core.Policies.Elements;

namespace Umbraco.Community.ContentChecks.Tests.Unit.Policies;

[TestFixture]
public class ContentCheckEffectiveChecksServiceTests
{
    [Test]
    public async Task GetEffectiveChecksAsync_TypeGovernedByAllThreeMechanisms_LabelsProvenanceCorrectly()
    {
        var xKey = Guid.NewGuid();
        var yKey = Guid.NewGuid();
        var zKey = Guid.NewGuid();
        var dataTypeKey = Guid.NewGuid();

        var contentTypeX = ContentTestFactory.ContentType(
            xKey,
            compositionKeys: [yKey],
            propertyTypes: [ContentTestFactory.PropertyType("content", "Umbraco.BlockList", dataTypeKey).Object]);
        var contentTypeZ = ContentTestFactory.ContentType(zKey);

        var contentTypeService = new Mock<IContentTypeService>();
        contentTypeService.Setup(s => s.GetAsync(xKey)).ReturnsAsync(contentTypeX.Object);
        contentTypeService.Setup(s => s.GetAsync(zKey)).ReturnsAsync(contentTypeZ.Object);

        var dataType = new Mock<IDataType>();
        dataType.SetupGet(d => d.ConfigurationObject).Returns(new object());
        var dataTypeService = new Mock<IDataTypeService>();
        dataTypeService.Setup(s => s.GetAsync(dataTypeKey)).ReturnsAsync(dataType.Object);

        var elementSource = new FakeContentCheckElementSource("Umbraco.BlockList", allowedElementTypeKeys: [zKey]);

        var policyService = new FakeContentCheckPolicyService();
        var directPolicy = new ContentCheckPolicy { Alias = "direct", Name = "Direct", ContentTypeKey = xKey, Rules = [] };
        var compositionPolicy = new ContentCheckPolicy { Alias = "composed", Name = "Composed", ContentTypeKey = yKey, Rules = [] };
        var cascadingPolicy = new ContentCheckPolicy { Alias = "cascaded", Name = "Cascaded", ContentTypeKey = zKey, Rules = [] };
        policyService.AddPolicy(directPolicy);
        policyService.AddPolicy(compositionPolicy);
        policyService.AddPolicy(cascadingPolicy);

        var service = new ContentCheckEffectiveChecksService(
            policyService,
            contentTypeService.Object,
            dataTypeService.Object,
            new ContentCheckElementSourceCollection(() => [elementSource]));

        var effectiveChecks = await service.GetEffectiveChecksAsync(xKey);

        Assert.That(effectiveChecks, Has.Count.EqualTo(3));

        var direct = effectiveChecks.Single(c => c.Policy.Alias == "direct");
        Assert.That(direct, Is.InstanceOf<ContentCheckEffectiveCheck.Direct>());
        Assert.That(direct.Provenance, Is.EqualTo(ContentCheckEffectiveCheckProvenance.Direct));

        var composed = effectiveChecks.Single(c => c.Policy.Alias == "composed");
        Assert.That(composed, Is.InstanceOf<ContentCheckEffectiveCheck.Composition>());
        var composition = (ContentCheckEffectiveCheck.Composition)composed;
        Assert.That(composition.Provenance, Is.EqualTo(ContentCheckEffectiveCheckProvenance.Composition));
        Assert.That(composition.ViaContentTypeKey, Is.EqualTo(yKey));

        var cascaded = effectiveChecks.Single(c => c.Policy.Alias == "cascaded");
        Assert.That(cascaded, Is.InstanceOf<ContentCheckEffectiveCheck.Cascading>());
        var cascading = (ContentCheckEffectiveCheck.Cascading)cascaded;
        Assert.That(cascading.Provenance, Is.EqualTo(ContentCheckEffectiveCheckProvenance.Cascading));
        Assert.That(cascading.ViaPropertyAlias, Is.EqualTo("content"));
        Assert.That(cascading.ViaElementTypeKey, Is.EqualTo(zKey));
    }

    [Test]
    public async Task GetEffectiveChecksAsync_SelfReferencingElementType_DoesNotRecurseForever()
    {
        // A Testimonial-shaped element type whose own Block List allows itself again -- the
        // cycle guard is scoped to the current path, not "ever visited anywhere", so this must
        // still terminate rather than recurse until the stack overflows.
        var pageKey = Guid.NewGuid();
        var dataTypeKey = Guid.NewGuid();

        var pageType = ContentTestFactory.ContentType(
            pageKey,
            propertyTypes: [ContentTestFactory.PropertyType("content", "Umbraco.BlockList", dataTypeKey).Object]);

        var contentTypeService = new Mock<IContentTypeService>();
        contentTypeService.Setup(s => s.GetAsync(pageKey)).ReturnsAsync(pageType.Object);

        var dataType = new Mock<IDataType>();
        dataType.SetupGet(d => d.ConfigurationObject).Returns(new object());
        var dataTypeService = new Mock<IDataTypeService>();
        dataTypeService.Setup(s => s.GetAsync(dataTypeKey)).ReturnsAsync(dataType.Object);

        // The property's own editor allows the page type itself -- a direct self-reference.
        var elementSource = new FakeContentCheckElementSource("Umbraco.BlockList", allowedElementTypeKeys: [pageKey]);

        var service = new ContentCheckEffectiveChecksService(
            new FakeContentCheckPolicyService(),
            contentTypeService.Object,
            dataTypeService.Object,
            new ContentCheckElementSourceCollection(() => [elementSource]));

        var effectiveChecks = await service.GetEffectiveChecksAsync(pageKey);

        Assert.That(effectiveChecks, Is.Empty);
    }
}
