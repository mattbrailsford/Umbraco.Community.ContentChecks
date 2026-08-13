using Umbraco.Community.ContentChecks.Core.Policies;

namespace Umbraco.Community.ContentChecks.Persistence.Policies;

/// <summary>
/// Maps between the domain model (<see cref="Guard"/>) and its EF Core-mapped entity.
/// </summary>
internal interface IGuardFactory
{
    /// <summary>
    /// Builds a domain guard from its entity.
    /// </summary>
    /// <param name="entity">The entity to map from.</param>
    /// <returns>The domain guard.</returns>
    Guard BuildDomain(GuardEntity entity);

    /// <summary>
    /// Builds a new entity from a domain guard.
    /// </summary>
    /// <param name="guard">The domain guard to map from.</param>
    /// <returns>The entity.</returns>
    GuardEntity BuildEntity(Guard guard);

    /// <summary>
    /// Updates an existing entity's fields from a domain guard.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="guard">The domain guard to update from.</param>
    void UpdateEntity(GuardEntity entity, Guard guard);
}
