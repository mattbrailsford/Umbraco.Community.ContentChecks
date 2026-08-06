using Umbraco.Cms.Core.Composing;
using Umbraco.Community.ContentChecks.Core.Config;

namespace Umbraco.Community.ContentChecks.Core.Checks;

/// <summary>
/// Defines a check that evaluates a single content snapshot and returns a status plus
/// actionable feedback. The interface is thin and non-generic so the runner and collection
/// builder can work with it uniformly; nobody implements it directly — see
/// <see cref="ContentCheckBase{TConfig}"/>.
/// </summary>
public interface IContentCheck : IDiscoverable
{
    /// <summary>
    /// The unique, code-registered identifier of the check.
    /// </summary>
    string Alias { get; }

    /// <summary>
    /// The display name of the check.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// The description of what this check evaluates.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Whether running the check is metered. What the runner orders and short-circuits on.
    /// </summary>
    ContentCheckCost Cost { get; }

    /// <summary>
    /// Whether the check's output is reproducible. What the result cache and the UI's trust
    /// disclosure key off.
    /// </summary>
    ContentCheckKind Kind { get; }

    /// <summary>
    /// The type that represents the check's configuration. Returns <c>null</c> if the check
    /// takes no configuration.
    /// </summary>
    Type? ConfigType { get; }

    /// <summary>
    /// Gets the configuration schema for UI rendering.
    /// </summary>
    /// <returns>The schema, or <c>null</c> if the check takes no configuration.</returns>
    ContentCheckConfigSchema? GetConfigSchema();

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
