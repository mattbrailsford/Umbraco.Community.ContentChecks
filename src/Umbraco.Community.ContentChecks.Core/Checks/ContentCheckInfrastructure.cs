using Umbraco.Community.ContentChecks.Core.Config;

namespace Umbraco.Community.ContentChecks.Core.Checks;

/// <summary>
/// Default implementation of <see cref="IContentCheckInfrastructure"/>.
/// </summary>
internal sealed class ContentCheckInfrastructure(
    IContentCheckConfigSchemaBuilder schemaBuilder,
    IContentCheckConfigResolver configResolver)
    : IContentCheckInfrastructure
{
    /// <inheritdoc />
    public IContentCheckConfigSchemaBuilder SchemaBuilder { get; } = schemaBuilder;

    /// <inheritdoc />
    public IContentCheckConfigResolver ConfigResolver { get; } = configResolver;
}
