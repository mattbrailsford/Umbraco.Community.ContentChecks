using Umbraco.Community.ContentChecks.Core.Checks;
using Umbraco.Community.ContentChecks.Core.Config;
using Umbraco.Community.ContentChecks.Core.Markup;

namespace Umbraco.Community.ContentChecks.Tests.Unit.Checks;

/// <summary>
/// A minimal <see cref="IMarkupContentCheck"/> test double that bypasses
/// <see cref="MarkupContentCheckBase{TConfig}"/> entirely -- mirrors <see cref="FakeContentCheck"/>
/// for the markup branch.
/// </summary>
internal sealed class FakeMarkupContentCheck : IMarkupContentCheck
{
    private readonly Func<ContentCheckMarkupContext, ContentCheckResult> _evaluate;

    public FakeMarkupContentCheck(
        string alias,
        ContentCheckCost cost,
        ContentCheckKind kind,
        Func<ContentCheckMarkupContext, ContentCheckResult> evaluate)
    {
        Alias = alias;
        Cost = cost;
        Kind = kind;
        _evaluate = evaluate;
    }

    public string Alias { get; }

    public string Name => Alias;

    public string Description => string.Empty;

    public ContentCheckCost Cost { get; }

    public ContentCheckKind Kind { get; }

    public Type? ConfigType => null;

    public int EvaluateCallCount { get; private set; }

    public ContentCheckConfigSchema? GetConfigSchema() => null;

    public Task<ContentCheckResult> EvaluateAsync(
        ContentCheckMarkupContext context,
        ContentCheckConfig config,
        CancellationToken cancellationToken)
    {
        EvaluateCallCount++;
        return Task.FromResult(_evaluate(context));
    }
}
