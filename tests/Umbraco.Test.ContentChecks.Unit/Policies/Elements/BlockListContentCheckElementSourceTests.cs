using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Umbraco.Cms.Core.Cache.PropertyEditors;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Community.ContentChecks.Core.Policies.Elements;

namespace Umbraco.Community.ContentChecks.Tests.Unit.Policies.Elements;

[TestFixture]
public class BlockListContentCheckElementSourceTests
{
    private static BlockListContentCheckElementSource CreateSource()
        => new(new Mock<IJsonSerializer>().Object, new Mock<IBlockEditorElementTypeCache>().Object, NullLogger<BlockListContentCheckElementSource>.Instance);

    [Test]
    public void GetAllowedElementTypeKeys_ReturnsContentAndSettingsElementTypeKeysForEveryBlock()
    {
        var testimonialKey = Guid.NewGuid();
        var testimonialSettingsKey = Guid.NewGuid();
        var ctaKey = Guid.NewGuid();

        var configuration = new BlockListConfiguration
        {
            Blocks =
            [
                new BlockListConfiguration.BlockConfiguration { ContentElementTypeKey = testimonialKey, SettingsElementTypeKey = testimonialSettingsKey },
                new BlockListConfiguration.BlockConfiguration { ContentElementTypeKey = ctaKey },
            ],
        };

        var source = CreateSource();

        var allowed = source.GetAllowedElementTypeKeys(configuration).ToList();

        Assert.That(allowed, Is.EquivalentTo(new[] { testimonialKey, testimonialSettingsKey, ctaKey }));
    }

    [Test]
    public void GetAllowedElementTypeKeys_NullConfiguration_ReturnsEmpty()
    {
        var source = CreateSource();

        Assert.That(source.GetAllowedElementTypeKeys(null), Is.Empty);
    }

    [Test]
    public void GetElementInstances_NullRawValue_ReturnsEmpty()
    {
        var source = CreateSource();

        Assert.That(source.GetElementInstances(null), Is.Empty);
    }
}
