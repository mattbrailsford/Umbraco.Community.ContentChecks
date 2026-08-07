using Moq;
using NUnit.Framework;
using Umbraco.Cms.Core.Events;
using Umbraco.Community.ContentChecks.Core.Policies;

namespace Umbraco.Community.ContentChecks.Tests.Unit.Policies;

[TestFixture]
public class ContentCheckPolicyServiceTests
{
    [Test]
    public void DeleteFolderAsync_FolderContainsAPolicy_ThrowsAndDoesNotDelete()
    {
        var folderKey = Guid.NewGuid();
        var repository = new Mock<IContentCheckPolicyRepository>();
        repository.Setup(r => r.PolicyExistsInFolderAsync(folderKey, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repository.Setup(r => r.FolderExistsInFolderAsync(folderKey, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var service = new ContentCheckPolicyService(repository.Object, Mock.Of<IEventAggregator>());

        Assert.That(async () => await service.DeleteFolderAsync(folderKey), Throws.InvalidOperationException);
        repository.Verify(r => r.DeleteFolderAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void DeleteFolderAsync_FolderContainsASubfolder_ThrowsAndDoesNotDelete()
    {
        var folderKey = Guid.NewGuid();
        var repository = new Mock<IContentCheckPolicyRepository>();
        repository.Setup(r => r.PolicyExistsInFolderAsync(folderKey, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        repository.Setup(r => r.FolderExistsInFolderAsync(folderKey, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var service = new ContentCheckPolicyService(repository.Object, Mock.Of<IEventAggregator>());

        Assert.That(async () => await service.DeleteFolderAsync(folderKey), Throws.InvalidOperationException);
        repository.Verify(r => r.DeleteFolderAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task DeleteFolderAsync_EmptyFolder_Deletes()
    {
        var folderKey = Guid.NewGuid();
        var repository = new Mock<IContentCheckPolicyRepository>();
        repository.Setup(r => r.PolicyExistsInFolderAsync(folderKey, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        repository.Setup(r => r.FolderExistsInFolderAsync(folderKey, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        repository.Setup(r => r.DeleteFolderAsync(folderKey, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var service = new ContentCheckPolicyService(repository.Object, Mock.Of<IEventAggregator>());

        var deleted = await service.DeleteFolderAsync(folderKey);

        Assert.That(deleted, Is.True);
        repository.Verify(r => r.DeleteFolderAsync(folderKey, It.IsAny<CancellationToken>()), Times.Once);
    }
}
