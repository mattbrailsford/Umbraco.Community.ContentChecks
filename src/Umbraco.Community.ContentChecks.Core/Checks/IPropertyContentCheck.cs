namespace Umbraco.Community.ContentChecks.Core.Checks;

/// <summary>
/// A check that evaluates a content snapshot's properties — see <see cref="ContentCheckContext"/>.
/// What every check has been, before markup checks (<see cref="IMarkupContentCheck"/>) existed.
/// Most checks implement this, via <see cref="PropertyContentCheckBase{TConfig}"/>.
/// </summary>
public interface IPropertyContentCheck : IContentCheck
{
    /// <summary>
    /// Evaluates the check against a single content snapshot.
    /// </summary>
    /// <param name="context">The content being evaluated, plus culture/segment.</param>
    /// <param name="config">The check's resolved configuration for this rule.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The evaluation result.</returns>
    Task<ContentCheckResult> EvaluateAsync(
        ContentCheckContext context,
        ContentCheckConfig config,
        CancellationToken cancellationToken);
}
