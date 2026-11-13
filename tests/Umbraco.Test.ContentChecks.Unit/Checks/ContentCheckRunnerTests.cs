using Moq;
using NUnit.Framework;
using Umbraco.Cms.Core.Events;
using Umbraco.Community.ContentChecks.Core.Checks;
using Umbraco.Community.ContentChecks.Core.Markup;

namespace Umbraco.Community.ContentChecks.Tests.Unit.Checks;

[TestFixture]
public class ContentCheckRunnerTests
{
    private InMemoryContentCheckResultCache _cache = null!;
    private Mock<IEventAggregator> _eventAggregator = null!;
    private Mock<IContentCheckMarkupProvider> _markupProvider = null!;
    private ContentCheckRunner _runner = null!;

    [SetUp]
    public void SetUp()
    {
        _cache = new InMemoryContentCheckResultCache();
        _eventAggregator = new Mock<IEventAggregator>();
        _markupProvider = new Mock<IContentCheckMarkupProvider>();
        _runner = new ContentCheckRunner(_cache, _eventAggregator.Object, _markupProvider.Object);
    }

    private static InMemoryContentCheckSnapshot CreateSnapshot()
        => new(Guid.NewGuid(), "page");

    private static ResolvedContentCheckRule Rule(
        FakeContentCheck check,
        RequirementLevel requirementLevel = RequirementLevel.Recommended)
        => new()
        {
            RuleKey = Guid.NewGuid(),
            Check = check,
            Config = new ContentCheckConfig(),
            RequirementLevel = requirementLevel,
        };

    private static ResolvedContentCheckRule Rule(
        FakeMarkupContentCheck check,
        RequirementLevel requirementLevel = RequirementLevel.Recommended)
        => new()
        {
            RuleKey = Guid.NewGuid(),
            Check = check,
            Config = new ContentCheckConfig(),
            RequirementLevel = requirementLevel,
        };

    [Test]
    public async Task RunAsync_MandatoryFreeFailure_ShortCircuitsMeteredRules()
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
                Rule(freeCheck, RequirementLevel.Mandatory),
                Rule(meteredCheck, RequirementLevel.Optional),
            ],
        };

        var report = await _runner.RunAsync(request, CancellationToken.None);

        var meteredEntry = report.Entries.Single(e => e.CheckAlias == "metered");
        Assert.That(meteredEntry.Result.Status, Is.EqualTo(ContentCheckStatus.NotEvaluated));
        Assert.That(meteredEntry.Result.NotEvaluatedReason, Is.EqualTo(ContentCheckNotEvaluatedReason.ShortCircuited));
        Assert.That(meteredCheck.EvaluateCallCount, Is.EqualTo(0), "The metered check must never actually run.");
    }

    [Test]
    public async Task RunAsync_NoMandatoryFreeFailure_RunsMeteredRules()
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
                Rule(freeCheck, RequirementLevel.Mandatory),
                Rule(meteredCheck, RequirementLevel.Optional),
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
    public async Task RunAsync_SameContentStampAndConfig_SecondRunHitsCache()
    {
        var check = PassingCheck();

        var snapshot = new StampedFakeSnapshot(Guid.NewGuid(), "page", contentStamp: "stamp-1");
        var request = new ContentCheckRunRequest { Content = snapshot, Rules = [Rule(check)] };

        await _runner.RunAsync(request, CancellationToken.None);
        await _runner.RunAsync(request, CancellationToken.None);

        Assert.That(check.EvaluateCallCount, Is.EqualTo(1), "The second run should hit the cache instead of re-evaluating.");
    }

    [Test]
    public async Task RunAsync_NoContentStamp_NeverCaches()
    {
        var check = PassingCheck();

        // A snapshot that can't say when its content last changed is never cached -- the one
        // remaining escape hatch, kept so such an implementation can't silently serve stale results.
        var snapshot = new StampedFakeSnapshot(Guid.NewGuid(), "page", contentStamp: null);
        var request = new ContentCheckRunRequest { Content = snapshot, Rules = [Rule(check)] };

        await _runner.RunAsync(request, CancellationToken.None);
        await _runner.RunAsync(request, CancellationToken.None);

        Assert.That(check.EvaluateCallCount, Is.EqualTo(2), "With no ContentStamp, every run must re-evaluate.");
    }

    /// <summary>
    /// The unit-level guard for the defect that shipped: the cache used to key on
    /// <c>VersionId</c>, which a draft save doesn't change, so editing a property and re-running
    /// returned the pre-edit verdict. The stamp has to move when the values do.
    /// </summary>
    [Test]
    public async Task RunAsync_ValuesChangedBetweenRuns_ReEvaluates()
    {
        var check = PassingCheck();

        var snapshot = CreateSnapshot().SetValue("title", "before");
        var request = new ContentCheckRunRequest { Content = snapshot, Rules = [Rule(check)] };

        await _runner.RunAsync(request, CancellationToken.None);
        snapshot.SetValue("title", "after");
        await _runner.RunAsync(request, CancellationToken.None);

        Assert.That(check.EvaluateCallCount, Is.EqualTo(2), "A changed value must invalidate the cached result.");
    }

    [Test]
    public async Task RunAsync_ValuesUnchangedBetweenRuns_HitsCache()
    {
        var check = PassingCheck();

        // The other half of the above: an in-memory snapshot with no persisted version used to skip
        // the cache entirely. It hashes its own values now, so unchanged content is cacheable.
        var snapshot = CreateSnapshot().SetValue("title", "unchanged");
        var request = new ContentCheckRunRequest { Content = snapshot, Rules = [Rule(check)] };

        await _runner.RunAsync(request, CancellationToken.None);
        await _runner.RunAsync(request, CancellationToken.None);

        Assert.That(check.EvaluateCallCount, Is.EqualTo(1), "Unchanged values should hit the cache.");
    }

    [Test]
    public async Task RunAsync_SameValueUnderDifferentCulture_DoesNotShareCacheEntry()
    {
        var check = PassingCheck();

        var danish = CreateSnapshot().SetValue("title", "same", culture: "da-DK");
        var english = CreateSnapshot().SetValue("title", "same", culture: "en-US");

        await _runner.RunAsync(
            new ContentCheckRunRequest { Content = danish, Rules = [Rule(check)] },
            CancellationToken.None);
        await _runner.RunAsync(
            new ContentCheckRunRequest { Content = english, Rules = [Rule(check)] },
            CancellationToken.None);

        Assert.That(
            check.EvaluateCallCount,
            Is.EqualTo(2),
            "A value on a different culture is different content, and must not share a cached result.");
    }

    [Test]
    public async Task RunAsync_NoMarkupRules_NeverCallsTheMarkupProvider()
    {
        var request = new ContentCheckRunRequest { Content = CreateSnapshot(), Rules = [Rule(PassingCheck())] };

        await _runner.RunAsync(request, CancellationToken.None);

        _markupProvider.Verify(
            p => p.GetMarkupAsync(It.IsAny<ContentCheckMarkupRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task RunAsync_MarkupRule_PassesTheFetchedMarkupToTheCheck()
    {
        var markup = new ContentCheckMarkupResult { Success = true, Html = "<html>fetched</html>" };
        _markupProvider
            .Setup(p => p.GetMarkupAsync(It.IsAny<ContentCheckMarkupRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(markup);

        ContentCheckMarkupContext? seenContext = null;
        var markupCheck = new FakeMarkupContentCheck(
            "markup",
            ContentCheckCost.Metered,
            ContentCheckKind.Deterministic,
            context =>
            {
                seenContext = context;
                return new ContentCheckResult { Status = ContentCheckStatus.Pass, Message = "ok" };
            });

        var request = new ContentCheckRunRequest { Content = CreateSnapshot(), Rules = [Rule(markupCheck)] };

        await _runner.RunAsync(request, CancellationToken.None);

        Assert.That(seenContext, Is.Not.Null);
        Assert.That(seenContext!.Markup, Is.SameAs(markup));
    }

    [Test]
    public async Task RunAsync_TwoMarkupRulesInOneRun_FetchesMarkupOnlyOnce()
    {
        _markupProvider
            .Setup(p => p.GetMarkupAsync(It.IsAny<ContentCheckMarkupRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContentCheckMarkupResult { Success = true, Html = "<html></html>" });

        var firstMarkupCheck = new FakeMarkupContentCheck(
            "markup-1", ContentCheckCost.Metered, ContentCheckKind.Deterministic,
            _ => new ContentCheckResult { Status = ContentCheckStatus.Pass, Message = "ok" });
        var secondMarkupCheck = new FakeMarkupContentCheck(
            "markup-2", ContentCheckCost.Metered, ContentCheckKind.Deterministic,
            _ => new ContentCheckResult { Status = ContentCheckStatus.Pass, Message = "ok" });

        var request = new ContentCheckRunRequest
        {
            Content = CreateSnapshot(),
            Rules = [Rule(firstMarkupCheck), Rule(secondMarkupCheck)],
        };

        await _runner.RunAsync(request, CancellationToken.None);

        _markupProvider.Verify(
            p => p.GetMarkupAsync(It.IsAny<ContentCheckMarkupRequest>(), It.IsAny<CancellationToken>()),
            Times.Once,
            "Two markup rules in the same run must share one fetch, not one each.");
    }

    [Test]
    public async Task RunAsync_MandatoryFreeFailure_ShortCircuitsAMeteredMarkupRule_WithoutFetching()
    {
        var freeCheck = new FakeContentCheck(
            "free-blocking", ContentCheckCost.Free, ContentCheckKind.Deterministic,
            _ => new ContentCheckResult { Status = ContentCheckStatus.Fail, Message = "failed" });
        var markupCheck = new FakeMarkupContentCheck(
            "markup", ContentCheckCost.Metered, ContentCheckKind.Deterministic,
            _ => new ContentCheckResult { Status = ContentCheckStatus.Pass, Message = "should not run" });

        var request = new ContentCheckRunRequest
        {
            Content = CreateSnapshot(),
            Rules = [Rule(freeCheck, RequirementLevel.Mandatory), Rule(markupCheck, RequirementLevel.Optional)],
        };

        await _runner.RunAsync(request, CancellationToken.None);

        Assert.That(markupCheck.EvaluateCallCount, Is.EqualTo(0));
        _markupProvider.Verify(
            p => p.GetMarkupAsync(It.IsAny<ContentCheckMarkupRequest>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "A markup fetch skipped by the short-circuit must never actually happen.");
    }

    private static FakeContentCheck PassingCheck()
        => new(
            "free",
            ContentCheckCost.Free,
            ContentCheckKind.Deterministic,
            _ => new ContentCheckResult { Status = ContentCheckStatus.Pass, Message = "ok" });

    private sealed class StampedFakeSnapshot : IContentCheckSnapshot
    {
        public StampedFakeSnapshot(Guid key, string contentTypeAlias, string? contentStamp)
        {
            Key = key;
            ContentTypeAlias = contentTypeAlias;
            ContentStamp = contentStamp;
        }

        public Guid Key { get; }

        public string ContentTypeAlias { get; }

        public int? VersionId => null;

        public string? ContentStamp { get; }

        public bool HasProperty(string alias) => false;

        public TValue? GetValue<TValue>(string alias, string? culture = null, string? segment = null) => default;
    }
}
