using Umbraco.Community.ContentChecks.Core.Policies;

namespace Umbraco.Community.ContentChecks.Persistence.Policies;

/// <summary>
/// Maps between the domain models (<see cref="ContentCheckPolicy"/>, <see cref="ContentCheckPolicyFolder"/>)
/// and their EF Core-mapped entities.
/// </summary>
internal interface IContentCheckPolicyFactory
{
    /// <summary>
    /// Builds a domain policy from its entity.
    /// </summary>
    /// <param name="entity">The entity to map from.</param>
    /// <returns>The domain policy.</returns>
    ContentCheckPolicy BuildDomain(ContentCheckPolicyEntity entity);

    /// <summary>
    /// Builds a new entity from a domain policy.
    /// </summary>
    /// <param name="policy">The domain policy to map from.</param>
    /// <returns>The entity.</returns>
    ContentCheckPolicyEntity BuildEntity(ContentCheckPolicy policy);

    /// <summary>
    /// Updates an existing entity's fields from a domain policy.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="policy">The domain policy to update from.</param>
    void UpdateEntity(ContentCheckPolicyEntity entity, ContentCheckPolicy policy);

    /// <summary>
    /// Builds a domain folder from its entity.
    /// </summary>
    /// <param name="entity">The entity to map from.</param>
    /// <returns>The domain folder.</returns>
    ContentCheckPolicyFolder BuildDomain(ContentCheckPolicyFolderEntity entity);

    /// <summary>
    /// Builds a new entity from a domain folder.
    /// </summary>
    /// <param name="folder">The domain folder to map from.</param>
    /// <returns>The entity.</returns>
    ContentCheckPolicyFolderEntity BuildEntity(ContentCheckPolicyFolder folder);

    /// <summary>
    /// Updates an existing entity's fields from a domain folder.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="folder">The domain folder to update from.</param>
    void UpdateEntity(ContentCheckPolicyFolderEntity entity, ContentCheckPolicyFolder folder);
}
