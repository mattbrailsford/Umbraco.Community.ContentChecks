using NUnit.Framework;
using Umbraco.Community.ContentChecks.Core.Checks;
using Umbraco.Community.ContentChecks.Core.Config;
using Umbraco.Community.ContentChecks.Core.Markup;

namespace Umbraco.Community.ContentChecks.Tests.Unit.Markup;

[TestFixture]
public class MarkupContentCheckBaseTests
{
    private EchoMarkupCheck _check = null!;

    [SetUp]
    public void SetUp() => _check = new EchoMarkupCheck(new FakeContentCheckInfrastructure());

    [Test]
    public async Task EvaluateAsync_FetchFailed_ReturnsNotEvaluatedWithoutCallingTheDerivedCheck()
    {
        var context = new ContentCheckMarkupContext
        {
            Content = new InMemoryContentCheckSnapshot(Guid.NewGuid(), "page"),
            Markup = new ContentCheckMarkupResult { Success = false, FailureReason = "could not fetch" },
        };

        var result = await _check.EvaluateAsync(context, new ContentCheckConfig(), CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ContentCheckStatus.NotEvaluated));
        Assert.That(result.NotEvaluatedReason, Is.EqualTo(ContentCheckNotEvaluatedReason.Error));
        Assert.That(result.Message, Is.EqualTo("could not fetch"));
        Assert.That(_check.EvaluateCallCount, Is.EqualTo(0));
    }

    [Test]
    public async Task EvaluateAsync_FetchFailed_NoFailureReasonGiven_UsesAGenericMessage()
    {
        var context = new ContentCheckMarkupContext
        {
            Content = new InMemoryContentCheckSnapshot(Guid.NewGuid(), "page"),
            Markup = new ContentCheckMarkupResult { Success = false },
        };

        var result = await _check.EvaluateAsync(context, new ContentCheckConfig(), CancellationToken.None);

        Assert.That(result.Message, Is.EqualTo("The page could not be fetched."));
    }

    [Test]
    public async Task EvaluateAsync_FetchSucceeded_CallsTheDerivedCheckWithTheFetchedHtml()
    {
        var context = new ContentCheckMarkupContext
        {
            Content = new InMemoryContentCheckSnapshot(Guid.NewGuid(), "page"),
            Markup = new ContentCheckMarkupResult { Success = true, Html = "<html>ok</html>" },
        };

        var result = await _check.EvaluateAsync(context, new ContentCheckConfig(), CancellationToken.None);

        Assert.That(_check.EvaluateCallCount, Is.EqualTo(1));
        Assert.That(result.Message, Is.EqualTo("<html>ok</html>"));
    }

    [ContentCheck("echo-markup", "Echo markup")]
    private sealed class EchoMarkupCheck : MarkupContentCheckBase<EchoMarkupCheckConfig>
    {
        public EchoMarkupCheck(IContentCheckInfrastructure infrastructure)
            : base(infrastructure)
        {
        }

        public int EvaluateCallCount { get; private set; }

        public override string Description => string.Empty;

        protected override Task<ContentCheckResult> EvaluateAsync(
            ContentCheckMarkupContext context, EchoMarkupCheckConfig config, CancellationToken cancellationToken)
        {
            EvaluateCallCount++;
            return Task.FromResult(new ContentCheckResult { Status = ContentCheckStatus.Pass, Message = context.Markup.Html! });
        }
    }

    private sealed class EchoMarkupCheckConfig
    {
    }

    private sealed class FakeContentCheckInfrastructure : IContentCheckInfrastructure
    {
        public IContentCheckConfigSchemaBuilder SchemaBuilder { get; } = new ContentCheckConfigSchemaBuilder();

        public IContentCheckConfigResolver ConfigResolver { get; } = new ContentCheckConfigResolver();
    }
}
