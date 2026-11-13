#pragma warning disable UMBRACOAI_DECISION // Decision capability is experimental -- see docs/ai-integration-spec.md.
using System.Text.Json;
using Moq;
using NUnit.Framework;
using Umbraco.AI.Core.Decision;
using Umbraco.Community.ContentChecks.AI.Checks;
using Umbraco.Community.ContentChecks.Core.Checks;
using Umbraco.Community.ContentChecks.Core.Config;

namespace Umbraco.Community.ContentChecks.Tests.Unit.Checks;

[TestFixture]
public class RubricContentCheckTests
{
    private Mock<IAIDecisionService> _decisionServiceMock = null!;
    private RubricContentCheck _check = null!;

    [SetUp]
    public void SetUp()
    {
        _decisionServiceMock = new Mock<IAIDecisionService>();
        _check = new RubricContentCheck(_decisionServiceMock.Object, new FakeContentCheckInfrastructure());
    }

    private static ContentCheckConfig ConfigFor(string propertyAlias, string rubric, double threshold = 0.7)
        => new()
        {
            Config = JsonSerializer.SerializeToElement(
                new RubricContentCheckConfig { PropertyAlias = propertyAlias, Rubric = rubric, Threshold = threshold },
                JsonSerializerOptions.Web),
        };

    private void SetUpJudgeResponse(double trueProbability)
        => _decisionServiceMock
            .Setup(s => s.AskAsync<AIBinaryDecisionAnswer>(
                It.IsAny<Action<AIDecisionBuilder>>(),
                It.IsAny<AIDecisionQuestion<AIBinaryDecisionAnswer>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIDecisionResponse<AIBinaryDecisionAnswer>
            {
                Answer = new AIBinaryDecisionAnswer { TrueProbability = trueProbability },
                Answers = new Dictionary<string, AIDecisionAnswer>(),
            });

    private void SetUpJudgeFailure(Exception exception)
        => _decisionServiceMock
            .Setup(s => s.AskAsync<AIBinaryDecisionAnswer>(
                It.IsAny<Action<AIDecisionBuilder>>(),
                It.IsAny<AIDecisionQuestion<AIBinaryDecisionAnswer>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

    [Test]
    public async Task EvaluateAsync_NoConfigStored_FallsBackToDefaultConfigInstance_ReturnsNotApplicable()
    {
        // No JSON at all -- ContentCheckBase<TConfig> defaults to `new RubricContentCheckConfig()`
        // (PropertyAlias = "", Rubric = "") itself, so this exercises that fallback.
        var context = new ContentCheckContext { Content = new InMemoryContentCheckSnapshot(Guid.NewGuid(), "page") };

        var result = await _check.EvaluateAsync(context, new ContentCheckConfig(), CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ContentCheckStatus.NotApplicable));
        _decisionServiceMock.VerifyNoOtherCalls();
    }

    [Test]
    public async Task EvaluateAsync_PropertyDoesNotExistOnContentType_ReturnsNotApplicable()
    {
        var context = new ContentCheckContext { Content = new InMemoryContentCheckSnapshot(Guid.NewGuid(), "page") };

        var result = await _check.EvaluateAsync(context, ConfigFor("body", "Be friendly and clear."), CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ContentCheckStatus.NotApplicable));
        _decisionServiceMock.VerifyNoOtherCalls();
    }

    [Test]
    public async Task EvaluateAsync_PropertyHasNoValue_ReturnsNotApplicable()
    {
        var snapshot = new InMemoryContentCheckSnapshot(Guid.NewGuid(), "page").SetValue("body", string.Empty);
        var context = new ContentCheckContext { Content = snapshot };

        var result = await _check.EvaluateAsync(context, ConfigFor("body", "Be friendly and clear."), CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ContentCheckStatus.NotApplicable));
        _decisionServiceMock.VerifyNoOtherCalls();
    }

    [Test]
    public async Task EvaluateAsync_ScoreAtOrAboveThreshold_ReturnsPass()
    {
        SetUpJudgeResponse(trueProbability: 0.9);
        var snapshot = new InMemoryContentCheckSnapshot(Guid.NewGuid(), "page").SetValue("body", "Great content.");
        var context = new ContentCheckContext { Content = snapshot };

        var result = await _check.EvaluateAsync(context, ConfigFor("body", "Be friendly and clear.", threshold: 0.7), CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ContentCheckStatus.Pass));
        Assert.That(result.Score, Is.EqualTo(0.9));
        Assert.That(result.Threshold, Is.EqualTo(0.7));
    }

    [Test]
    public async Task EvaluateAsync_ScoreBelowThreshold_ReturnsFail()
    {
        SetUpJudgeResponse(trueProbability: 0.2);
        var snapshot = new InMemoryContentCheckSnapshot(Guid.NewGuid(), "page").SetValue("body", "Bad content.");
        var context = new ContentCheckContext { Content = snapshot };

        var result = await _check.EvaluateAsync(context, ConfigFor("body", "Be friendly and clear.", threshold: 0.7), CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ContentCheckStatus.Fail));
        Assert.That(result.Score, Is.EqualTo(0.2));
    }

    [Test]
    public async Task EvaluateAsync_JudgeThrows_ReturnsNotEvaluatedWithErrorReason()
    {
        SetUpJudgeFailure(new InvalidOperationException("provider outage"));
        var snapshot = new InMemoryContentCheckSnapshot(Guid.NewGuid(), "page").SetValue("body", "Some content.");
        var context = new ContentCheckContext { Content = snapshot };

        var result = await _check.EvaluateAsync(context, ConfigFor("body", "Be friendly and clear."), CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ContentCheckStatus.NotEvaluated));
        Assert.That(result.NotEvaluatedReason, Is.EqualTo(ContentCheckNotEvaluatedReason.Error));
    }

    [Test]
    public void EvaluateAsync_JudgeCancelled_PropagatesCancellationRatherThanReportingAVerdict()
    {
        SetUpJudgeFailure(new OperationCanceledException());
        var snapshot = new InMemoryContentCheckSnapshot(Guid.NewGuid(), "page").SetValue("body", "Some content.");
        var context = new ContentCheckContext { Content = snapshot };

        Assert.ThrowsAsync<OperationCanceledException>(() =>
            _check.EvaluateAsync(context, ConfigFor("body", "Be friendly and clear."), CancellationToken.None));
    }

    private sealed class FakeContentCheckInfrastructure : IContentCheckInfrastructure
    {
        public IContentCheckConfigSchemaBuilder SchemaBuilder { get; } = new ContentCheckConfigSchemaBuilder();

        public IContentCheckConfigResolver ConfigResolver { get; } = new ContentCheckConfigResolver();
    }
}
