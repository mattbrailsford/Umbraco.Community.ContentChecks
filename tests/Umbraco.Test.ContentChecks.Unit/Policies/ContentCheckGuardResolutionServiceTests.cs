using Moq;
using NUnit.Framework;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Services;
using Umbraco.Community.ContentChecks.Core.Checks;
using Umbraco.Community.ContentChecks.Core.Policies;
using Umbraco.Community.ContentChecks.Tests.Unit.Checks;

namespace Umbraco.Community.ContentChecks.Tests.Unit.Policies;

[TestFixture]
public class ContentCheckGuardResolutionServiceTests
{
    [Test]
    public async Task ResolveResponseAsync_NoGuardConfigured_AlwaysWarns()
    {
        var repository = new InMemoryGuardRepository();
        var service = new ContentCheckGuardResolutionService(repository, Mock.Of<IEntityService>(), Mock.Of<IUserService>());
        var content = ContentTestFactory.Content(Guid.NewGuid()).Object;

        var response = await service.ResolveResponseAsync(content, RequirementLevel.Mandatory, currentUserKey: null);

        Assert.That(response, Is.EqualTo(ContentCheckResponse.Warn));
    }

    [Test]
    public async Task ResolveResponseAsync_GuardMatchesEveryoneRule_UsesItsBlockAt()
    {
        var content = ContentTestFactory.Content(Guid.NewGuid()).Object;

        var repository = new FakeGuardRepository(
        [
            new Guard
            {
                Name = "Site-wide",
                Rules = [new GuardRule { UserKey = null, BlockAt = RequirementLevel.Mandatory }],
            },
        ]);

        var service = new ContentCheckGuardResolutionService(repository, Mock.Of<IEntityService>(), Mock.Of<IUserService>());

        var response = await service.ResolveResponseAsync(content, RequirementLevel.Mandatory, currentUserKey: Guid.NewGuid());

        Assert.That(response, Is.EqualTo(ContentCheckResponse.Block));
    }

    [Test]
    public async Task ResolveResponseAsync_UserSpecificRule_BeatsEveryoneRule()
    {
        var content = ContentTestFactory.Content(Guid.NewGuid()).Object;
        var trustedUserKey = Guid.NewGuid();

        var repository = new FakeGuardRepository(
        [
            new Guard
            {
                Name = "Site-wide",
                Rules =
                [
                    new GuardRule { UserKey = trustedUserKey, BlockAt = null },
                    new GuardRule { UserKey = null, BlockAt = RequirementLevel.Mandatory },
                ],
            },
        ]);

        var service = new ContentCheckGuardResolutionService(repository, Mock.Of<IEntityService>(), Mock.Of<IUserService>());

        var response = await service.ResolveResponseAsync(content, RequirementLevel.Mandatory, currentUserKey: trustedUserKey);

        Assert.That(response, Is.EqualTo(ContentCheckResponse.Warn), "A trusted user's own rule must beat the catch-all rule.");
    }

    [Test]
    public async Task ResolveResponseAsync_RoleRule_BeatsEveryoneRule()
    {
        var content = ContentTestFactory.Content(Guid.NewGuid()).Object;
        var currentUserKey = Guid.NewGuid();
        var roleKey = Guid.NewGuid();

        var repository = new FakeGuardRepository(
        [
            new Guard
            {
                Name = "Site-wide",
                Rules =
                [
                    new GuardRule { RoleKey = roleKey, BlockAt = null },
                    new GuardRule { BlockAt = RequirementLevel.Mandatory },
                ],
            },
        ]);

        var userService = new Mock<IUserService>();
        userService.Setup(s => s.GetAsync(currentUserKey)).ReturnsAsync(UserWithGroup(roleKey));

        var service = new ContentCheckGuardResolutionService(repository, Mock.Of<IEntityService>(), userService.Object);

        var response = await service.ResolveResponseAsync(content, RequirementLevel.Mandatory, currentUserKey);

        Assert.That(response, Is.EqualTo(ContentCheckResponse.Warn), "A role rule must beat the catch-all rule.");
    }

    [Test]
    public async Task ResolveResponseAsync_UserSpecificRule_BeatsRoleRule()
    {
        var content = ContentTestFactory.Content(Guid.NewGuid()).Object;
        var currentUserKey = Guid.NewGuid();
        var roleKey = Guid.NewGuid();

        var repository = new FakeGuardRepository(
        [
            new Guard
            {
                Name = "Site-wide",
                Rules =
                [
                    new GuardRule { UserKey = currentUserKey, BlockAt = null },
                    new GuardRule { RoleKey = roleKey, BlockAt = RequirementLevel.Mandatory },
                ],
            },
        ]);

        var userService = new Mock<IUserService>();
        userService.Setup(s => s.GetAsync(currentUserKey)).ReturnsAsync(UserWithGroup(roleKey));

        var service = new ContentCheckGuardResolutionService(repository, Mock.Of<IEntityService>(), userService.Object);

        var response = await service.ResolveResponseAsync(content, RequirementLevel.Mandatory, currentUserKey);

        Assert.That(response, Is.EqualTo(ContentCheckResponse.Warn), "A user-specific rule must beat a matching role rule.");
    }

    private static IUser UserWithGroup(Guid roleKey)
    {
        var group = new Mock<IReadOnlyUserGroup>();
        group.SetupGet(g => g.Key).Returns(roleKey);

        var user = new Mock<IUser>();
        user.SetupGet(u => u.Groups).Returns([group.Object]);
        return user.Object;
    }

    [Test]
    public async Task ResolveResponseAsync_BlockAtNull_NeverBlocks()
    {
        var content = ContentTestFactory.Content(Guid.NewGuid()).Object;

        var repository = new FakeGuardRepository(
        [
            new Guard
            {
                Name = "Site-wide",
                Rules = [new GuardRule { UserKey = null, BlockAt = null }],
            },
        ]);

        var service = new ContentCheckGuardResolutionService(repository, Mock.Of<IEntityService>(), Mock.Of<IUserService>());

        var response = await service.ResolveResponseAsync(content, RequirementLevel.Mandatory, currentUserKey: null);

        Assert.That(response, Is.EqualTo(ContentCheckResponse.Warn));
    }

    [Test]
    public async Task ResolveResponseAsync_LevelBelowBlockAt_Warns()
    {
        var content = ContentTestFactory.Content(Guid.NewGuid()).Object;

        var repository = new FakeGuardRepository(
        [
            new Guard
            {
                Name = "Site-wide",
                Rules = [new GuardRule { UserKey = null, BlockAt = RequirementLevel.Mandatory }],
            },
        ]);

        var service = new ContentCheckGuardResolutionService(repository, Mock.Of<IEntityService>(), Mock.Of<IUserService>());

        // Optional is below the rule's Mandatory threshold -- must warn, not block.
        var response = await service.ResolveResponseAsync(content, RequirementLevel.Optional, currentUserKey: null);

        Assert.That(response, Is.EqualTo(ContentCheckResponse.Warn));
    }

    [Test]
    public async Task ResolveResponseAsync_LevelAboveBlockAt_Blocks()
    {
        var content = ContentTestFactory.Content(Guid.NewGuid()).Object;

        var repository = new FakeGuardRepository(
        [
            new Guard
            {
                Name = "Site-wide",
                Rules = [new GuardRule { UserKey = null, BlockAt = RequirementLevel.Recommended }],
            },
        ]);

        var service = new ContentCheckGuardResolutionService(repository, Mock.Of<IEntityService>(), Mock.Of<IUserService>());

        // Mandatory is above the rule's Recommended threshold -- must block too, not just the exact level.
        var response = await service.ResolveResponseAsync(content, RequirementLevel.Mandatory, currentUserKey: null);

        Assert.That(response, Is.EqualTo(ContentCheckResponse.Block));
    }

    private sealed class FakeGuardRepository : IGuardRepository
    {
        private readonly IReadOnlyList<Guard> _guards;

        public FakeGuardRepository(IReadOnlyList<Guard> guards) => _guards = guards;

        public Task<IReadOnlyList<Guard>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_guards);

        public Task<Guard?> GetByKeyAsync(Guid key, CancellationToken cancellationToken = default)
            => Task.FromResult(_guards.FirstOrDefault(s => s.Key == key));

        public Task<Guard> SaveAsync(Guard guard, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<bool> DeleteAsync(Guid key, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
