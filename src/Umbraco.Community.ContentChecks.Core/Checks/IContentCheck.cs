using Umbraco.Cms.Core.Composing;
using Umbraco.Community.ContentChecks.Core.Config;

namespace Umbraco.Community.ContentChecks.Core.Checks;

/// <summary>
/// What every check declares about itself, regardless of what it evaluates. Metadata only — no
/// <c>EvaluateAsync</c> here, since a property check and a markup check take different input
/// shapes (see <see cref="IPropertyContentCheck"/>/<see cref="IMarkupContentCheck"/>) and
/// shouldn't share one signature. A bare <see cref="IContentCheck"/> describes something but
/// can't run anything; nobody implements this interface directly — see
/// <see cref="ContentCheckMetadataBase{TConfig}"/>.
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
}
