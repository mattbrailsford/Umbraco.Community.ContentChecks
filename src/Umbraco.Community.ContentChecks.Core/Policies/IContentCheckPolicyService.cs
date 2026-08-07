namespace Umbraco.Community.ContentChecks.Core.Policies;

/// <summary>
/// CRUD and lookup for <see cref="ContentCheckPolicy"/> and its <see cref="ContentCheckPolicyFolder"/>
/// tree. One service for both — a folder has no lifecycle independent of the policy tree it
/// organizes.
/// </summary>
public interface IContentCheckPolicyService
{
    /// <summary>
    /// Gets a policy by its key.
    /// </summary>
    /// <param name="key">The policy's key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The policy, or <c>null</c> if not found.</returns>
    Task<ContentCheckPolicy?> GetPolicyAsync(Guid key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a policy by its alias.
    /// </summary>
    /// <param name="alias">The policy's alias.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The policy, or <c>null</c> if not found.</returns>
    Task<ContentCheckPolicy?> GetPolicyByAliasAsync(string alias, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets every policy.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Every policy.</returns>
    Task<IReadOnlyList<ContentCheckPolicy>> GetAllPoliciesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets every policy whose <see cref="ContentCheckPolicy.ContentTypeKey"/> is one of the given keys —
    /// used to resolve direct and composition matches for a content type in one query.
    /// </summary>
    /// <param name="contentTypeKeys">The content type keys to match against.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Every policy whose <see cref="ContentCheckPolicy.ContentTypeKey"/> is one of <paramref name="contentTypeKeys"/>.</returns>
    Task<IReadOnlyList<ContentCheckPolicy>> GetPoliciesByContentTypeKeysAsync(IReadOnlyCollection<Guid> contentTypeKeys, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates a policy. Fires <see cref="ContentCheckPolicySavingNotification"/> before,
    /// and <see cref="ContentCheckPolicySavedNotification"/> after.
    /// </summary>
    /// <param name="policy">The policy to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The saved policy.</returns>
    /// <exception cref="InvalidOperationException">Another policy already uses this alias.</exception>
    Task<ContentCheckPolicy> SavePolicyAsync(ContentCheckPolicy policy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a policy. Fires <see cref="ContentCheckPolicyDeletingNotification"/> before, and
    /// <see cref="ContentCheckPolicyDeletedNotification"/> after. Returns <c>false</c> if no policy
    /// with this key exists.
    /// </summary>
    /// <param name="key">The policy's key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if a policy was deleted; otherwise <c>false</c>.</returns>
    Task<bool> DeletePolicyAsync(Guid key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a folder by its key.
    /// </summary>
    /// <param name="key">The folder's key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The folder, or <c>null</c> if not found.</returns>
    Task<ContentCheckPolicyFolder?> GetFolderAsync(Guid key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets every folder.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Every folder.</returns>
    Task<IReadOnlyList<ContentCheckPolicyFolder>> GetAllFoldersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates a folder. Fires <see cref="ContentCheckPolicyFolderSavingNotification"/>
    /// before, and <see cref="ContentCheckPolicyFolderSavedNotification"/> after.
    /// </summary>
    /// <param name="folder">The folder to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The saved folder.</returns>
    Task<ContentCheckPolicyFolder> SaveFolderAsync(ContentCheckPolicyFolder folder, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a folder. Fires <see cref="ContentCheckPolicyFolderDeletingNotification"/> before,
    /// and <see cref="ContentCheckPolicyFolderDeletedNotification"/> after. Returns <c>false</c> if
    /// no folder with this key exists. Blocked, not cascading, when the folder still has policies
    /// or subfolders — an editor must empty it first.
    /// </summary>
    /// <param name="key">The folder's key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if a folder was deleted; otherwise <c>false</c>.</returns>
    /// <exception cref="InvalidOperationException">The folder still contains policies or subfolders.</exception>
    Task<bool> DeleteFolderAsync(Guid key, CancellationToken cancellationToken = default);
}
