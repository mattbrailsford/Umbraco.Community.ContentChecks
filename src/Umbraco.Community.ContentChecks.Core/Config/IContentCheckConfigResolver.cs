namespace Umbraco.Community.ContentChecks.Core.Config;

/// <summary>
/// Resolves a check's stored configuration (JSON) into a typed instance, validating it against
/// the check's schema when one is supplied.
/// </summary>
public interface IContentCheckConfigResolver
{
    /// <summary>
    /// Resolves stored configuration data to a typed instance.
    /// </summary>
    /// <typeparam name="TConfig">The configuration type to resolve to.</typeparam>
    /// <param name="data">The stored data (a <see cref="System.Text.Json.JsonElement"/>, a typed object, or <c>null</c>).</param>
    /// <param name="schema">The schema to validate against, or <c>null</c> to skip validation.</param>
    /// <returns>The resolved configuration, or <c>null</c> if <paramref name="data"/> was <c>null</c>.</returns>
    TConfig? ResolveModel<TConfig>(object? data, ContentCheckConfigSchema? schema = null)
        where TConfig : class, new();

    /// <summary>
    /// Resolves stored configuration data to an instance of a runtime-known type. Used when the
    /// configuration type is only known via <see cref="Umbraco.Community.ContentChecks.Core.Checks.IContentCheck.ConfigType"/>,
    /// which a generic call site can't constrain to <c>class, new()</c>.
    /// </summary>
    /// <param name="configType">The configuration type to resolve to.</param>
    /// <param name="data">The stored data (a <see cref="System.Text.Json.JsonElement"/>, a typed object, or <c>null</c>).</param>
    /// <param name="schema">The schema to validate against, or <c>null</c> to skip validation.</param>
    /// <returns>The resolved configuration, or <c>null</c> if <paramref name="data"/> was <c>null</c>.</returns>
    object? ResolveModel(Type configType, object? data, ContentCheckConfigSchema? schema = null);
}
