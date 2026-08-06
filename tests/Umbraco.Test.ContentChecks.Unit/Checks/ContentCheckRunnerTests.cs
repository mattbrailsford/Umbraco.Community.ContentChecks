using Moq;
using NUnit.Framework;
using Umbraco.Cms.Core.Events;
using Umbraco.Community.ContentChecks.Core.Checks;

namespace Umbraco.Community.ContentChecks.Tests.Unit.Checks;

[TestFixture]
public class ContentCheckRunnerTests
{
    private InMemoryContentCheckResultCache _cache = null!;
    private Mock<IEventAggregator> _eventAggregator = null!;
    private ContentCheckRunner _runner = null!;

    [SetUp]
    public void SetUp()
    {
        _cache = new InMemoryContentCheckResultCache();
        _eventAggregator = new Mock<IEventAggregator>();
        _runner = new ContentCheckRunner(_cache, _eventAggregator.Object);
    }

    private static InMemoryContentCheckSnapshot CreateSnapshot(int? versionId = null)
    {
        var snapshot = new InMemoryContentCheckSnapshot(Guid.NewGuid(), "page");
        return snapshot;
    }

    private static ResolvedContentCheckRule Rule(
        FakeContentCheck check,
        ContentCheckRuleMode mode = ContentCheckRuleMode.Advisory)
        => new()
        {
            Check = check,
            Config = new ContentCheckConfig(),
            Mode = mode,
        };

    [Test]
    public async Task RunAsync_BlockingFreeFailure_ShortCircuitsMeteredRules()
    {
        var freeCheck = new FakeContentCheck(
            "free-blocking",
            ContentCheckCost.Free,
            ContentCheckKind.Deterministic,
            _ => new ContentCheckResult { Status = ContentCheckStatus.Fail, Message = "failed" });

        var meteredCheck = new FakeContentCheck(
            "metered",
            ContentCheckCost.Metered,
            ContentCheckKind.Nondeterministic,
            _ => new ContentCheckResult { Status = ContentCheckStatus.Pass, Message = "should not run" });

        var request = new ContentCheckRunRequest
        {
            Content = CreateSnapshot(),
            Rules =
            [
                Rule(freeCheck, ContentCheckRuleMode.Blocking),
                Rule(meteredCheck, ContentCheckRuleMode.Advisory),
            ],
        };

        var report = await _runner.RunAsync(request, CancellationToken.None);

        var meteredEntry = report.Entries.Single(e => e.CheckAlias == "metered");
        Assert.That(meteredEntry.Result.Status, Is.EqualTo(ContentCheckStatus.NotEvaluated));
        Assert.That(meteredEntry.Result.NotEvaluatedReason, Is.EqualTo(ContentCheckNotEvaluatedReason.ShortCircuited));
        Assert.That(meteredCheck.EvaluateCallCount, Is.EqualTo(0), "The metered check must never actually run.");
    }

    [Test]
    public async Task RunAsync_NoBlockingFreeFailure_RunsMeteredRules()
    {
        var freeCheck = new FakeContentCheck(
            "free",
            ContentCheckCost.Free,
            ContentCheckKind.Deterministic,
            _ => new ContentCheckResult { Status = ContentCheckStatus.Pass, Message = "ok" });

        var meteredCheck = new FakeContentCheck(
            "metered",
            ContentCheckCost.Metered,
            ContentCheckKind.Nondeterministic,
            _ => new ContentCheckResult { Status = ContentCheckStatus.Pass, Message = "ran" });

        var request = new ContentCheckRunRequest
        {
            Content = CreateSnapshot(),
            Rules =
            [
                Rule(freeCheck, ContentCheckRuleMode.Blocking),
                Rule(meteredCheck, ContentCheckRuleMode.Advisory),
            ],
        };

        var report = await _runner.RunAsync(request, CancellationToken.None);

        var meteredEntry = report.Entries.Single(e => e.CheckAlias == "metered");
        Assert.That(meteredEntry.Result.Status, Is.EqualTo(ContentCheckStatus.Pass));
        Assert.That(meteredCheck.EvaluateCallCount, Is.EqualTo(1));
    }

    [Test]
    public async Task RunAsync_PublishesReportCompletedNotification()
    {
        var check = new FakeContentCheck(
            "free",
            ContentCheckCost.Free,
            ContentCheckKind.Deterministic,
            _ => new ContentCheckResult { Status = ContentCheckStatus.Pass, Message = "ok" });

        var request = new ContentCheckRunRequest { Content = CreateSnapshot(), Rules = [Rule(check)] };

        await _runner.RunAsync(request, CancellationToken.None);

        _eventAggregator.Verify(
            x => x.PublishAsync(It.IsAny<ContentCheckReportCompletedNotification>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task RunAsync_SameVersionedContentAndConfig_SecondRunHitsCache()
    {
        var check = new FakeContentCheck(
            "free",
            ContentCheckCost.Free,
            ContentCheckKind.Deterministic,
            _ => new ContentCheckResult { Status = ContentCheckStatus.Pass, Message = "ok" });

        var snapshot = new VersionedFakeSnapshot(Guid.NewGuid(), "page", versionId: 42);
        var request = new ContentCheckRunRequest { Content = snapshot, Rules = [Rule(check)] };

        await _runner.RunAsync(request, CancellationToken.None);
        await _runner.RunAsync(request, CancellationToken.None);

        Assert.That(check.EvaluateCallCount, Is.EqualTo(1), "The second run should hit the cache instead of re-evaluating.");
    }

    [Test]
    public async Task RunAsync_NoPersistedVersion_NeverCaches()
    {
        var check = new FakeContentCheck(
            "free",
            ContentCheckCost.Free,
            ContentCheckKind.Deterministic,
            _ => new ContentCheckResult { Status = ContentCheckStatus.Pass, Message = "ok" });

        // InMemoryContentCheckSnapshot.VersionId is always null -- represents unsaved editor
        // state / a Draft Set's pending state, which the cache must skip entirely.
        var request = new ContentCheckRunRequest { Content = CreateSnapshot(), Rules = [Rule(check)] };

        await _runner.RunAsync(request, CancellationToken.None);
        await _runner.RunAsync(request, CancellationToken.None);

        Assert.That(check.EvaluateCallCount, Is.EqualTo(2), "With no VersionId, every run must re-evaluate.");
    }

    private sealed class VersionedFakeSnapshot : IContentCheckSnapshot
    {
        public VersionedFakeSnapshot(Guid key, string contentTypeAlias, int versionId)
        {
            Key = key;
            ContentTypeAlias = contentTypeAlias;
            VersionId = versionId;
        }

        public Guid Key { get; }

        public string ContentTypeAlias { get; }

        public int? VersionId { get; }

        public bool HasProperty(string alias) => false;

        public TValue? GetValue<TValue>(string alias, string? culture = null, string? segment = null) => default;
    }
}
