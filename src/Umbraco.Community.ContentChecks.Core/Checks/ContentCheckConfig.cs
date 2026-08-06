using System.Text.Json;

namespace Umbraco.Community.ContentChecks.Core.Checks;

/// <summary>
/// The raw configuration for a single rule, passed to <see cref="IContentCheck.EvaluateAsync"/>
/// separately from the content being checked — mirrors <c>AIGuardrailConfig</c>: the evaluation
/// subject and the check's own configuration are two differently-shaped parameters, not one
/// bag. A wrapper rather than a bare <see cref="JsonElement"/> so a future field can be added
/// here without changing <c>EvaluateAsync</c>'s signature — the one method every check
/// overrides, and so the one place a breaking change would be most expensive.
/// </summary>
/// <remarks>
/// <see cref="ContentCheckBase{TConfig}"/> resolves this into a typed <c>TConfig</c> before a
/// check ever sees it — a hand-written <see cref="IContentCheck"/> implementer (there are none
/// today; <see cref="IContentCheck"/>'s own docs say nobody implements it directly) would need
/// its own JSON deserialization here, same as it already needs its own everything else.
/// </remarks>
public sealed class ContentCheckConfig
{
    /// <summary>
    /// The rule's configuration as a JSON element, or <c>null</c> if the check takes no
    /// configuration.
    /// </summary>
    public JsonElement? Config { get; init; }
}
