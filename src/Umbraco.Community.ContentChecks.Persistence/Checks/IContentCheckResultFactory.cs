using Umbraco.Community.ContentChecks.Core.Checks;

namespace Umbraco.Community.ContentChecks.Persistence.Checks;

/// <summary>
/// Maps between the domain model (<see cref="ContentCheckStoredResult"/>) and its EF Core-mapped
/// entity. No update-in-place mapping — unlike Policy/Guard, stored results are always replaced
/// wholesale (delete + insert), never updated, so there's nothing to update onto an existing row.
/// </summary>
internal interface IContentCheckResultFactory
{
    /// <summary>
    /// Builds a domain result from its entity.
    /// </summary>
    /// <param name="entity">The entity to map from.</param>
    /// <returns>The domain result.</returns>
    ContentCheckStoredResult BuildDomain(ContentCheckResultEntity entity);

    /// <summary>
    /// Builds a new entity from a domain result.
    /// </summary>
    /// <param name="result">The domain result to map from.</param>
    /// <returns>The entity.</returns>
    ContentCheckResultEntity BuildEntity(ContentCheckStoredResult result);
}
