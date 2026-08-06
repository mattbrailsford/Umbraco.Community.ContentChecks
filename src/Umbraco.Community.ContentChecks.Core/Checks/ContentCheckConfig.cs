using System.Text.Json;

namespace Umbraco.Community.ContentChecks.Core.Checks;

/// <summary>
/// The resolved configuration for a single rule, passed to a check's <c>EvaluateAsync</c>
/// separately from the content being checked — mirrors <c>AIGuardrailConfig</c>: the evaluation
/// subject and the check's own configuration are two differently-shaped parameters, not one
/// bag.
/// </summary>
public sealed class ContentCheckConfig
{
    /// <summary>
    /// The rule's configuration as a JSON element, or <c>null</c> if the check takes no
    /// configuration.
    /// </summary>
    public JsonElement? Config { get; init; }

    /// <summary>
    /// Deserializes the configuration to the specified type.
    /// </summary>
    /// <typeparam name="T">The configuration type.</typeparam>
    /// <returns>The deserialized configuration, or <c>default</c> if none is present.</returns>
    public T? Deserialize<T>()
        => Config is { ValueKind: not JsonValueKind.Undefined }
            ? Config.Value.Deserialize<T>(JsonSerializerOptions.Web)
            : default;
}
