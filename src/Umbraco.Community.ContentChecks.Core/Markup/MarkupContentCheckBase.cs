using Umbraco.Community.ContentChecks.Core.Checks;

namespace Umbraco.Community.ContentChecks.Core.Markup;

/// <summary>
/// What you actually write a markup check by inheriting — the markup counterpart of
/// <see cref="PropertyContentCheckBase{TConfig}"/>.
/// </summary>
/// <typeparam name="TConfig">The configuration type for this check.</typeparam>
public abstract class MarkupContentCheckBase<TConfig> : ContentCheckMetadataBase<TConfig>, IMarkupContentCheck
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MarkupContentCheckBase{TConfig}"/> class.
    /// </summary>
    /// <param name="infrastructure">The check infrastructure dependencies.</param>
    protected MarkupContentCheckBase(IContentCheckInfrastructure infrastructure)
        : base(infrastructure)
    {
    }

    /// <inheritdoc />
    /// <remarks>
    /// Short-circuits to <see cref="ContentCheckStatus.NotEvaluated"/> when the fetch itself
    /// failed, the same category of boilerplate <see cref="PropertyContentCheckBase{TConfig}"/>
    /// already centralizes for config resolution — a real check's body can assume valid HTML,
    /// the same way a property check assumes a resolved, defaulted config.
    /// </remarks>
    public Task<ContentCheckResult> EvaluateAsync(
        ContentCheckMarkupContext context,
        ContentCheckConfig config,
        CancellationToken cancellationToken)
    {
        if (!context.Markup.Success)
        {
            return Task.FromResult(new ContentCheckResult
            {
                Status = ContentCheckStatus.NotEvaluated,
                Message = context.Markup.FailureReason ?? "The page could not be fetched.",
                NotEvaluatedReason = ContentCheckNotEvaluatedReason.Error,
            });
        }

        return EvaluateAsync(context, ResolveConfig(config) ?? CreateDefaultConfig(), cancellationToken);
    }

    /// <summary>
    /// Evaluates the check against a content item's rendered markup, with its configuration
    /// already resolved to <typeparamref name="TConfig"/> and defaulted if none was stored, and
    /// with <see cref="ContentCheckMarkupContext.Markup"/> already known to have succeeded. What
    /// a check author actually overrides.
    /// </summary>
    /// <param name="context">The fetched markup, plus the content it was fetched for and the culture/segment.</param>
    /// <param name="config">The check's resolved, typed configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The evaluation result.</returns>
    protected abstract Task<ContentCheckResult> EvaluateAsync(
        ContentCheckMarkupContext context,
        TConfig config,
        CancellationToken cancellationToken);
}
