using Umbraco.Community.ContentChecks.Core.Checks;
using Umbraco.Community.ContentChecks.Core.Config;

namespace Umbraco.Community.ContentChecks.Tests.Unit.Checks;

/// <summary>
/// A minimal <see cref="IPropertyContentCheck"/> test double that bypasses
/// <see cref="PropertyContentCheckBase{TConfig}"/> entirely -- the runner only depends on the
/// interface, so tests exercising the runner don't need real attribute/schema plumbing.
/// </summary>
internal sealed class FakeContentCheck : IPropertyContentCheck
{
    private readonly Func<ContentCheckContext, ContentCheckResult> _evaluate;

    public FakeContentCheck(
        string alias,
        ContentCheckCost cost,
        ContentCheckKind kind,
        Func<ContentCheckContext, ContentCheckResult> evaluate)
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
        ContentCheckContext context,
        ContentCheckConfig config,
        CancellationToken cancellationToken)
    {
        EvaluateCallCount++;
        return Task.FromResult(_evaluate(context));
    }
}
