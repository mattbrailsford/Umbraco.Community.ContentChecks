using Moq;
using NUnit.Framework;
using Umbraco.Cms.Core.Models;
using Umbraco.Community.ContentChecks.Core.Checks;
using Umbraco.Community.ContentChecks.Core.Policies;

namespace Umbraco.Community.ContentChecks.Tests.Unit.Policies;

[TestFixture]
public class ContentCheckPublishGateServiceTests
{
    private const string CurrentStamp = "1000";
    private const string CheckAlias = "myCheck";
    private static readonly Guid RuleKey = Guid.NewGuid();

    [Test]
    public async Task EvaluateAsync_RealFailure_ResolvesGuardAndBlocks()
    {
        var content = Content();
        var resolutionService = ResolutionServiceReturning(Target(CheckAlias, RequirementLevel.Mandatory));
        var resultService = ResultServiceReturning(StoredResult(ContentCheckStatus.Fail));
        var guardService = GuardServiceAlwaysReturning(ContentCheckResponse.Block);

        var service = new ContentCheckPublishGateService(resultService.Object, resolutionService.Object, guardService.Object);
        var result = await service.EvaluateAsync(content, culture: null, segment: null, currentUserKey: Guid.NewGuid());

        Assert.That(result.IsBlocked, Is.True);
        Assert.That(result.BlockingEntries.Single().CheckAlias, Is.EqualTo(CheckAlias));
    }

    [Test]
    public async Task EvaluateAsync_RealFailure_GuardWarns_DoesNotBlock()
    {
        var content = Content();
        var resolutionService = ResolutionServiceReturning(Target(CheckAlias, RequirementLevel.Mandatory));
        var resultService = ResultServiceReturning(StoredResult(ContentCheckStatus.Fail));
        var guardService = GuardServiceAlwaysReturning(ContentCheckResponse.Warn);

        var service = new ContentCheckPublishGateService(resultService.Object, resolutionService.Object, guardService.Object);
        var result = await service.EvaluateAsync(content, culture: null, segment: null, currentUserKey: null);

        Assert.That(result.IsBlocked, Is.False);
    }

    [Test]
    public async Task EvaluateAsync_NotEvaluated_FailOpen_NeverBlocks_EvenIfGuardWouldBlock()
    {
        var content = Content();
        var resolutionService = ResolutionServiceReturning(
            Target(CheckAlias, RequirementLevel.Mandatory, ContentCheckNotEvaluatedPolicy.FailOpen));
        var resultService = ResultServiceReturning(StoredResult(ContentCheckStatus.NotEvaluated));
        var guardService = GuardServiceAlwaysReturning(ContentCheckResponse.Block);

        var service = new ContentCheckPublishGateService(resultService.Object, resolutionService.Object, guardService.Object);
        var result = await service.EvaluateAsync(content, culture: null, segment: null, currentUserKey: null);

        Assert.That(result.IsBlocked, Is.False, "FailOpen is the default -- a missed check must never block.");
        guardService.Verify(
            g => g.ResolveResponseAsync(It.IsAny<IContent>(), It.IsAny<RequirementLevel>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "A FailOpen NotEvaluated entry shouldn't even reach Guard.");
    }

    [Test]
    public async Task EvaluateAsync_NotEvaluated_FailClosed_CountsAsFailureAtItsOwnRequirementLevel()
    {
        var content = Content();
        var resolutionService = ResolutionServiceReturning(
            Target(CheckAlias, RequirementLevel.Recommended, ContentCheckNotEvaluatedPolicy.FailClosed));
        var resultService = ResultServiceReturning(StoredResult(ContentCheckStatus.NotEvaluated));

        RequirementLevel? levelPassedToGuard = null;
        var guardService = new Mock<IGuardResolutionService>();
        guardService
            .Setup(g => g.ResolveResponseAsync(It.IsAny<IContent>(), It.IsAny<RequirementLevel>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .Callback<IContent, RequirementLevel, Guid?, CancellationToken>((_, level, _, _) => levelPassedToGuard = level)
            .ReturnsAsync(ContentCheckResponse.Block);

        var service = new ContentCheckPublishGateService(resultService.Object, resolutionService.Object, guardService.Object);
        var result = await service.EvaluateAsync(content, culture: null, segment: null, currentUserKey: null);

        Assert.That(result.IsBlocked, Is.True);
        Assert.That(levelPassedToGuard, Is.EqualTo(RequirementLevel.Recommended), "FailClosed must not invent a new severity -- it uses the rule's own level.");
    }

    [Test]
    public async Task EvaluateAsync_StoredReportIsStale_TreatedLikeNotEvaluated()
    {
        var content = Content();
        var resolutionService = ResolutionServiceReturning(
            Target(CheckAlias, RequirementLevel.Mandatory, ContentCheckNotEvaluatedPolicy.FailOpen));
        var staleResult = StoredResult(ContentCheckStatus.Fail, contentStamp: "old-stamp");
        var resultService = ResultServiceReturning(staleResult);
        var guardService = GuardServiceAlwaysReturning(ContentCheckResponse.Block);

        var service = new ContentCheckPublishGateService(resultService.Object, resolutionService.Object, guardService.Object);
        var result = await service.EvaluateAsync(content, culture: null, segment: null, currentUserKey: null);

        Assert.That(result.IsBlocked, Is.False, "A stale report is treated like NotEvaluated -- FailOpen here means it doesn't block.");
    }

    [Test]
    public async Task EvaluateAsync_NoStoredResultAtAll_FailClosedRule_BlocksTheSameAsNotEvaluated()
    {
        // Nothing has ever run for this content -- a Guard that's meant to require a passing
        // report must not be dodgeable just by publishing unchecked content.
        var content = Content();
        var resolutionService = ResolutionServiceReturning(
            Target(CheckAlias, RequirementLevel.Mandatory, ContentCheckNotEvaluatedPolicy.FailClosed));
        var resultService = ResultServiceReturning();
        var guardService = GuardServiceAlwaysReturning(ContentCheckResponse.Block);

        var service = new ContentCheckPublishGateService(resultService.Object, resolutionService.Object, guardService.Object);
        var result = await service.EvaluateAsync(content, culture: null, segment: null, currentUserKey: null);

        Assert.That(result.IsBlocked, Is.True, "A required rule with no report at all must block the same as a FailClosed NotEvaluated result.");
    }

    [Test]
    public async Task EvaluateAsync_NoStoredResultAtAll_FailOpenRule_DoesNotBlock()
    {
        var content = Content();
        var resolutionService = ResolutionServiceReturning(
            Target(CheckAlias, RequirementLevel.Mandatory, ContentCheckNotEvaluatedPolicy.FailOpen));
        var resultService = ResultServiceReturning();
        var guardService = GuardServiceAlwaysReturning(ContentCheckResponse.Block);

        var service = new ContentCheckPublishGateService(resultService.Object, resolutionService.Object, guardService.Object);
        var result = await service.EvaluateAsync(content, culture: null, segment: null, currentUserKey: null);

        Assert.That(result.IsBlocked, Is.False);
    }

    [Test]
    public async Task EvaluateAsync_PassingEntry_NeverReachesGuard()
    {
        var content = Content();
        var resolutionService = ResolutionServiceReturning(Target(CheckAlias, RequirementLevel.Mandatory));
        var resultService = ResultServiceReturning(StoredResult(ContentCheckStatus.Pass));
        var guardService = new Mock<IGuardResolutionService>();

        var service = new ContentCheckPublishGateService(resultService.Object, resolutionService.Object, guardService.Object);
        var result = await service.EvaluateAsync(content, culture: null, segment: null, currentUserKey: null);

        Assert.That(result.IsBlocked, Is.False);
        guardService.Verify(
            g => g.ResolveResponseAsync(It.IsAny<IContent>(), It.IsAny<RequirementLevel>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task EvaluateAsync_StoredResultForARuleThatNoLongerApplies_IsIgnored()
    {
        // The stored report has a result for a check the policy no longer resolves to (e.g. the
        // rule was removed since the last run) -- only what's currently resolved should matter.
        var content = Content();
        var resolutionService = ResolutionServiceReturning();
        var resultService = ResultServiceReturning(StoredResult(ContentCheckStatus.Fail));
        var guardService = new Mock<IGuardResolutionService>();

        var service = new ContentCheckPublishGateService(resultService.Object, resolutionService.Object, guardService.Object);
        var result = await service.EvaluateAsync(content, culture: null, segment: null, currentUserKey: null);

        Assert.That(result.IsBlocked, Is.False);
        guardService.Verify(
            g => g.ResolveResponseAsync(It.IsAny<IContent>(), It.IsAny<RequirementLevel>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task EvaluateAsync_RecommendedLevelFailure_ReportedAsRecommendedMissed_RegardlessOfGuard()
    {
        var content = Content();
        var resolutionService = ResolutionServiceReturning(Target(CheckAlias, RequirementLevel.Recommended));
        var resultService = ResultServiceReturning(StoredResult(ContentCheckStatus.Fail));
        // No Guard would ever block a Recommended-level failure here (Warn for everyone), but the
        // miss should still be reported -- it's a fact about the content, not an enforcement outcome.
        var guardService = GuardServiceAlwaysReturning(ContentCheckResponse.Warn);

        var service = new ContentCheckPublishGateService(resultService.Object, resolutionService.Object, guardService.Object);
        var result = await service.EvaluateAsync(content, culture: null, segment: null, currentUserKey: null);

        Assert.That(result.IsBlocked, Is.False);
        Assert.That(result.RecommendedMissedEntries.Single().CheckAlias, Is.EqualTo(CheckAlias));
    }

    [Test]
    public async Task EvaluateAsync_MandatoryLevelFailure_NeverCountsAsRecommendedMissed()
    {
        var content = Content();
        var resolutionService = ResolutionServiceReturning(Target(CheckAlias, RequirementLevel.Mandatory));
        var resultService = ResultServiceReturning(StoredResult(ContentCheckStatus.Fail));
        var guardService = GuardServiceAlwaysReturning(ContentCheckResponse.Warn);

        var service = new ContentCheckPublishGateService(resultService.Object, resolutionService.Object, guardService.Object);
        var result = await service.EvaluateAsync(content, culture: null, segment: null, currentUserKey: null);

        Assert.That(result.RecommendedMissedEntries, Is.Empty, "Only Recommended-level misses are reported this way.");
    }

    private static IContent Content()
    {
        var mock = new Mock<IContent>();
        mock.SetupGet(c => c.Key).Returns(Guid.NewGuid());
        mock.SetupGet(c => c.UpdateDate).Returns(new DateTime(long.Parse(CurrentStamp), DateTimeKind.Utc));
        return mock.Object;
    }

    // No RequirementLevel/NotEvaluatedPolicy here -- a stored result no longer carries either,
    // since the gate resolves both live from the current rule (via Target below) rather than from
    // a frozen snapshot. See ContentCheckPublishGateService.IsCountedAsFailing.
    private static ContentCheckStoredResult StoredResult(
        ContentCheckStatus status,
        string contentStamp = CurrentStamp) => new()
    {
        ContentKey = Guid.NewGuid(),
        ContentStamp = contentStamp,
        Path = [],
        CheckAlias = CheckAlias,
        RuleKey = RuleKey,
        Result = new ContentCheckResult { Status = status, Message = "some message" },
        RanAtUtc = new DateTime(2026, 8, 13, 0, 0, 0, DateTimeKind.Utc),
    };

    private static ContentCheckResolvedTarget Target(
        string checkAlias,
        RequirementLevel requirementLevel,
        ContentCheckNotEvaluatedPolicy notEvaluatedPolicy = ContentCheckNotEvaluatedPolicy.FailOpen)
    {
        var check = new Mock<IContentCheck>();
        check.SetupGet(c => c.Alias).Returns(checkAlias);

        var rule = new ResolvedContentCheckRule
        {
            RuleKey = RuleKey,
            Check = check.Object,
            Config = new ContentCheckConfig(),
            RequirementLevel = requirementLevel,
            NotEvaluatedPolicy = notEvaluatedPolicy,
        };

        return new ContentCheckResolvedTarget
        {
            Snapshot = Mock.Of<IContentCheckSnapshot>(),
            PolicyMatches =
            [
                new ContentCheckResolvedPolicyMatch
                {
                    Policy = new ContentCheckPolicy { Alias = "policy", Name = "Policy", ContentTypeKey = Guid.NewGuid() },
                    Provenance = ContentCheckEffectiveCheckProvenance.Direct,
                    Rules = [rule],
                },
            ],
            Path = [],
        };
    }

    private static Mock<IContentCheckResultService> ResultServiceReturning(params ContentCheckStoredResult[] results)
    {
        var mock = new Mock<IContentCheckResultService>();
        mock.Setup(s => s.GetByContentKeyAsync(It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(results);
        return mock;
    }

    private static Mock<IContentCheckResolutionService> ResolutionServiceReturning(params ContentCheckResolvedTarget[] targets)
    {
        var mock = new Mock<IContentCheckResolutionService>();
        mock.Setup(s => s.ResolveAsync(It.IsAny<IContent>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(targets);
        return mock;
    }

    private static Mock<IGuardResolutionService> GuardServiceAlwaysReturning(ContentCheckResponse response)
    {
        var mock = new Mock<IGuardResolutionService>();
        mock.Setup(g => g.ResolveResponseAsync(It.IsAny<IContent>(), It.IsAny<RequirementLevel>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
        return mock;
    }
}
