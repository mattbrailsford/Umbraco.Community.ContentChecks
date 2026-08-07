namespace Umbraco.Community.ContentChecks.Core.Policies;

/// <summary>
/// Persistence for <see cref="ContentCheckPolicy"/> and its <see cref="ContentCheckPolicyFolder"/>
/// tree. One repository for both, since a folder has no lifecycle independent of the policy tree
/// it organizes. Internal implementation detail — use <see cref="IContentCheckPolicyService"/> for
/// external access; nothing outside the service touches this directly.
/// </summary>
internal interface IContentCheckPolicyRepository
{
    /// <summary>
    /// Gets a policy by its key.
    /// </summary>
    /// <param name="key">The policy's key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The policy, or <c>null</c> if not found.</returns>
    Task<ContentCheckPolicy?> GetByKeyAsync(Guid key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a policy by its alias.
    /// </summary>
    /// <param name="alias">The policy's alias.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The policy, or <c>null</c> if not found.</returns>
    Task<ContentCheckPolicy?> GetByAliasAsync(string alias, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets every policy.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Every policy.</returns>
    Task<IReadOnlyList<ContentCheckPolicy>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets every policy whose <see cref="ContentCheckPolicy.ContentTypeKey"/> is one of the given keys.
    /// </summary>
    /// <param name="contentTypeKeys">The content type keys to match against.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Every matching policy.</returns>
    Task<IReadOnlyList<ContentCheckPolicy>> GetByContentTypeKeysAsync(IReadOnlyCollection<Guid> contentTypeKeys, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates a policy.
    /// </summary>
    /// <param name="policy">The policy to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The saved policy.</returns>
    Task<ContentCheckPolicy> SaveAsync(ContentCheckPolicy policy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a policy.
    /// </summary>
    /// <param name="key">The policy's key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if a policy was deleted; otherwise <c>false</c>.</returns>
    Task<bool> DeleteAsync(Guid key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether any policy has the given folder as its <see cref="ContentCheckPolicy.ParentFolderKey"/>
    /// — an existence check, not a full load, so a folder-emptiness check doesn't pay for every
    /// policy in the system just to answer yes/no.
    /// </summary>
    /// <param name="folderKey">The folder's key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if at least one policy lives directly in this folder.</returns>
    Task<bool> PolicyExistsInFolderAsync(Guid folderKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a folder by its key.
    /// </summary>
    /// <param name="key">The folder's key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The folder, or <c>null</c> if not found.</returns>
    Task<ContentCheckPolicyFolder?> GetFolderByKeyAsync(Guid key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets every folder.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Every folder.</returns>
    Task<IReadOnlyList<ContentCheckPolicyFolder>> GetAllFoldersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates a folder.
    /// </summary>
    /// <param name="folder">The folder to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The saved folder.</returns>
    Task<ContentCheckPolicyFolder> SaveFolderAsync(ContentCheckPolicyFolder folder, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a folder.
    /// </summary>
    /// <param name="key">The folder's key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if a folder was deleted; otherwise <c>false</c>.</returns>
    Task<bool> DeleteFolderAsync(Guid key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether any folder has the given folder as its own
    /// <see cref="ContentCheckPolicyFolder.ParentFolderKey"/> — an existence check, not a full
    /// load, same reasoning as <see cref="PolicyExistsInFolderAsync"/>.
    /// </summary>
    /// <param name="folderKey">The folder's key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if at least one subfolder lives directly in this folder.</returns>
    Task<bool> FolderExistsInFolderAsync(Guid folderKey, CancellationToken cancellationToken = default);
}
