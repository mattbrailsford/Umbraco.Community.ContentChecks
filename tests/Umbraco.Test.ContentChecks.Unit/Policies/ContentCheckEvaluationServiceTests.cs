using Moq;
using NUnit.Framework;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Community.ContentChecks.Core.Checks;
using Umbraco.Community.ContentChecks.Core.Policies;
using Umbraco.Community.ContentChecks.Tests.Unit.Checks;

namespace Umbraco.Community.ContentChecks.Tests.Unit.Policies;

[TestFixture]
public class ContentCheckEvaluationServiceTests
{
    [Test]
    public async Task EvaluateAsync_ZipsEachEntryWithItsTargetPath_AndComputesOverallStatus()
    {
        var contentKey = Guid.NewGuid();
        var content = ContentTestFactory.Content(Guid.NewGuid(), key: contentKey).Object;

        var passingCheck = new FakeContentCheck(
            "required-property",
            ContentCheckCost.Free,
            ContentCheckKind.Deterministic,
            _ => new ContentCheckResult { Status = ContentCheckStatus.Pass, Message = "ok" });
        var failingCheck = new FakeContentCheck(
            "quote-required",
            ContentCheckCost.Free,
            ContentCheckKind.Deterministic,
            _ => new ContentCheckResult { Status = ContentCheckStatus.Fail, Message = "missing" });

        var rootPolicy = new ContentCheckPolicy { Alias = "root", Name = "Root", ContentTypeKey = Guid.NewGuid(), Rules = [] };
        var testimonialPolicy = new ContentCheckPolicy { Alias = "testimonial", Name = "Testimonial", ContentTypeKey = Guid.NewGuid(), Rules = [] };

        var rootTarget = new ContentCheckResolvedTarget
        {
            Snapshot = new InMemoryContentCheckSnapshot(contentKey, "landingPage"),
            PolicyMatches =
            [
                new ContentCheckResolvedPolicyMatch
                {
                    Policy = rootPolicy,
                    Provenance = ContentCheckEffectiveCheckProvenance.Direct,
                    Rules = [new ResolvedContentCheckRule { RuleKey = Guid.NewGuid(), Check = passingCheck, Config = new ContentCheckConfig(), RequirementLevel = RequirementLevel.Recommended }],
                },
            ],
            Path = [],
        };

        var testimonialKey = Guid.NewGuid();
        var testimonialStep = new ContentCheckLocationStep { PropertyAlias = "content", ElementKey = testimonialKey };
        var nestedTarget = new ContentCheckResolvedTarget
        {
            Snapshot = new InMemoryContentCheckSnapshot(testimonialKey, "testimonial"),
            PolicyMatches =
            [
                new ContentCheckResolvedPolicyMatch
                {
                    Policy = testimonialPolicy,
                    Provenance = ContentCheckEffectiveCheckProvenance.Cascading,
                    Rules = [new ResolvedContentCheckRule { RuleKey = Guid.NewGuid(), Check = failingCheck, Config = new ContentCheckConfig(), RequirementLevel = RequirementLevel.Mandatory }],
                },
            ],
            Path = [testimonialStep],
        };

        var resolutionService = new Mock<IContentCheckResolutionService>();
        resolutionService
            .Setup(s => s.ResolveAsync(content, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([rootTarget, nestedTarget]);

        var runner = new ContentCheckRunner(new InMemoryContentCheckResultCache(), Mock.Of<IEventAggregator>());
        var service = new ContentCheckEvaluationService(resolutionService.Object, runner);

        var progress = new RecordingProgress<ContentCheckEvaluationProgress>();
        var report = await service.EvaluateAsync(content, progress: progress);

        Assert.That(report.ContentKey, Is.EqualTo(contentKey));
        Assert.That(report.Entries, Has.Count.EqualTo(2));

        var rootEntry = report.Entries.Single(e => e.Entry.CheckAlias == "required-property");
        Assert.That(rootEntry.Path, Is.Empty);

        var nestedEntry = report.Entries.Single(e => e.Entry.CheckAlias == "quote-required");
        Assert.That(nestedEntry.Path, Is.EqualTo(new[] { testimonialStep }));

        Assert.That(report.OverallStatus, Is.EqualTo(ContentCheckStatus.Fail));

        Assert.That(progress.Reports.Select(p => p.TargetsCompleted), Is.EqualTo(new[] { 0, 1, 2 }));
        Assert.That(progress.Reports, Has.All.Matches<ContentCheckEvaluationProgress>(p => p.TargetsTotal == 2));
    }

    [Test]
    public async Task EvaluateAsync_StampsTheReportWithTheRootContentsOwnContentStamp()
    {
        var contentKey = Guid.NewGuid();
        var content = ContentTestFactory.Content(Guid.NewGuid(), key: contentKey).Object;

        var resolutionService = new Mock<IContentCheckResolutionService>();
        resolutionService
            .Setup(s => s.ResolveAsync(content, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var runner = new ContentCheckRunner(new InMemoryContentCheckResultCache(), Mock.Of<IEventAggregator>());
        var service = new ContentCheckEvaluationService(resolutionService.Object, runner);

        var report = await service.EvaluateAsync(content);

        // Never re-derive the stamp -- a report's ContentStamp must be exactly what a snapshot of
        // this same content would produce, since a later reader compares them for equality.
        var expectedStamp = new ContentCheckSnapshot(content).ContentStamp;
        Assert.That(report.ContentStamp, Is.EqualTo(expectedStamp));
    }

    [Test]
    public async Task EvaluateAsync_NoResolvedTargets_ReturnsEmptyReport_OverallStatusIsNotApplicable()
    {
        var content = ContentTestFactory.Content(Guid.NewGuid()).Object;

        var resolutionService = new Mock<IContentCheckResolutionService>();
        resolutionService
            .Setup(s => s.ResolveAsync(content, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var runner = new ContentCheckRunner(new InMemoryContentCheckResultCache(), Mock.Of<IEventAggregator>());
        var service = new ContentCheckEvaluationService(resolutionService.Object, runner);

        var report = await service.EvaluateAsync(content);

        Assert.That(report.Entries, Is.Empty);
        Assert.That(report.OverallStatus, Is.EqualTo(ContentCheckStatus.NotApplicable));
    }

    private sealed class RecordingProgress<T> : IProgress<T>
    {
        public List<T> Reports { get; } = [];

        public void Report(T value) => Reports.Add(value);
    }
}
