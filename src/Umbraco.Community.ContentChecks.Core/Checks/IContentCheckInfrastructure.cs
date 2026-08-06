using Umbraco.Community.ContentChecks.Core.Config;

namespace Umbraco.Community.ContentChecks.Core.Checks;

/// <summary>
/// The infrastructure dependencies every <see cref="ContentCheckBase{TConfig}"/> needs, bundled
/// into one constructor parameter so adding a future dependency doesn't break every existing
/// check's constructor.
/// </summary>
public interface IContentCheckInfrastructure
{
    /// <summary>
    /// Builder for check configuration schemas.
    /// </summary>
    IContentCheckConfigSchemaBuilder SchemaBuilder { get; }

    /// <summary>
    /// Resolver for converting stored configuration to typed models.
    /// </summary>
    IContentCheckConfigResolver ConfigResolver { get; }
}
