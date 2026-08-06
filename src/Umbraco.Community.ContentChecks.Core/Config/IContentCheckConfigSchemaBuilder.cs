namespace Umbraco.Community.ContentChecks.Core.Config;

/// <summary>
/// Builds a <see cref="ContentCheckConfigSchema"/> from a check's configuration type by
/// reflecting over its <see cref="ContentCheckFieldAttribute"/>-decorated properties.
/// </summary>
public interface IContentCheckConfigSchemaBuilder
{
    /// <summary>
    /// Builds a schema for the specified configuration type.
    /// </summary>
    /// <typeparam name="TConfig">The configuration type to build a schema for.</typeparam>
    /// <param name="checkAlias">The alias of the check the schema belongs to.</param>
    ContentCheckConfigSchema BuildForType<TConfig>(string checkAlias)
        where TConfig : class;

    /// <summary>
    /// Builds a schema for the specified configuration type.
    /// </summary>
    /// <param name="configType">The configuration type to build a schema for.</param>
    /// <param name="checkAlias">The alias of the check the schema belongs to.</param>
    ContentCheckConfigSchema BuildForType(Type configType, string checkAlias);
}
