namespace Umbraco.Community.ContentChecks.Core.Policies;

/// <summary>
/// CRUD and lookup for <see cref="Guard"/>. Flat list — no folders, unlike
/// <see cref="IContentCheckPolicyService"/>'s policy/folder tree. See
/// docs/guard-spec.md.
/// </summary>
public interface IGuardService
{
    /// <summary>
    /// Gets a guard by its key.
    /// </summary>
    /// <param name="key">The guard's key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The guard, or <c>null</c> if not found.</returns>
    Task<Guard?> GetByKeyAsync(Guid key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets every guard.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Every guard.</returns>
    Task<IReadOnlyList<Guard>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates a guard. Fires <see cref="GuardSavingNotification"/>
    /// before, and <see cref="GuardSavedNotification"/> after.
    /// </summary>
    /// <param name="guard">The guard to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The saved guard.</returns>
    /// <exception cref="InvalidOperationException">The save was cancelled by a notification handler.</exception>
    Task<Guard> SaveAsync(Guard guard, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a guard. Fires <see cref="GuardDeletingNotification"/> before, and
    /// <see cref="GuardDeletedNotification"/> after. Returns <c>false</c> if no
    /// guard with this key exists.
    /// </summary>
    /// <param name="key">The guard's key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if a guard was deleted; otherwise <c>false</c>.</returns>
    Task<bool> DeleteAsync(Guid key, CancellationToken cancellationToken = default);
}
