namespace Umbraco.Community.ContentChecks.Core.Checks;

/// <summary>
/// What you actually write a property check by inheriting. Renamed from <c>ContentCheckBase</c>
/// once a markup branch (<see cref="Markup.MarkupContentCheckBase{TConfig}"/>) existed — with two
/// branches, the old name no longer meant "the base for any check" (free to rename: nothing has
/// shipped). One constructor only — no legacy overload, since there's nothing to be
/// backward-compatible with in a greenfield package.
/// </summary>
/// <typeparam name="TConfig">The configuration type for this check.</typeparam>
public abstract class PropertyContentCheckBase<TConfig> : ContentCheckMetadataBase<TConfig>, IPropertyContentCheck
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PropertyContentCheckBase{TConfig}"/> class.
    /// </summary>
    /// <param name="infrastructure">The check infrastructure dependencies.</param>
    protected PropertyContentCheckBase(IContentCheckInfrastructure infrastructure)
        : base(infrastructure)
    {
    }

    /// <inheritdoc />
    /// <remarks>
    /// Resolves the raw <see cref="ContentCheckConfig"/> into a typed, defaulted <c>TConfig</c>
    /// once, then hands it to <see cref="EvaluateAsync(ContentCheckContext, TConfig, CancellationToken)"/>
    /// — every real check overrides that one instead, so the "resolve, then fall back to a
    /// default instance" step doesn't get repeated in every check's own body.
    /// </remarks>
    public Task<ContentCheckResult> EvaluateAsync(
        ContentCheckContext context,
        ContentCheckConfig config,
        CancellationToken cancellationToken)
        => EvaluateAsync(context, ResolveConfig(config) ?? CreateDefaultConfig(), cancellationToken);

    /// <summary>
    /// Evaluates the check against a single content snapshot, with its configuration already
    /// resolved to <typeparamref name="TConfig"/> and defaulted (via the config type's own
    /// property initializers) if none was stored. What a check author actually overrides.
    /// </summary>
    /// <param name="context">The content being evaluated, plus culture/segment.</param>
    /// <param name="config">The check's resolved, typed configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The evaluation result.</returns>
    protected abstract Task<ContentCheckResult> EvaluateAsync(
        ContentCheckContext context,
        TConfig config,
        CancellationToken cancellationToken);
}
