using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Umbraco.Cms.Core.Cache.PropertyEditors;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Community.ContentChecks.Core.Policies.Elements;

namespace Umbraco.Community.ContentChecks.Tests.Unit.Policies.Elements;

[TestFixture]
public class BlockGridContentCheckElementSourceTests
{
    private static BlockGridContentCheckElementSource CreateSource()
        => new(new Mock<IJsonSerializer>().Object, new Mock<IBlockEditorElementTypeCache>().Object, NullLogger<BlockGridContentCheckElementSource>.Instance);

    [Test]
    public void GetAllowedElementTypeKeys_ReturnsContentAndSettingsElementTypeKeysForEveryBlock_IgnoringAreaAllowLists()
    {
        var heroKey = Guid.NewGuid();
        var heroSettingsKey = Guid.NewGuid();
        var featureCardKey = Guid.NewGuid();

        var configuration = new BlockGridConfiguration
        {
            Blocks =
            [
                new BlockGridConfiguration.BlockGridBlockConfiguration
                {
                    ContentElementTypeKey = heroKey,
                    SettingsElementTypeKey = heroSettingsKey,
                    Areas = [],
                },
                new BlockGridConfiguration.BlockGridBlockConfiguration
                {
                    ContentElementTypeKey = featureCardKey,
                    Areas = [],
                },
            ],
        };

        var source = CreateSource();

        var allowed = source.GetAllowedElementTypeKeys(configuration).ToList();

        Assert.That(allowed, Is.EquivalentTo(new[] { heroKey, heroSettingsKey, featureCardKey }));
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
