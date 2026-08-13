namespace Umbraco.Community.ContentChecks.Core.Policies;

/// <summary>
/// Persistence for <see cref="Guard"/>. Internal implementation detail — use
/// <see cref="IGuardService"/> for external access; nothing outside the service
/// touches this directly. See docs/guard-spec.md.
/// </summary>
internal interface IGuardRepository
{
    /// <summary>
    /// Gets every guard.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Every guard.</returns>
    Task<IReadOnlyList<Guard>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a guard by its key.
    /// </summary>
    /// <param name="key">The guard's key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The guard, or <c>null</c> if not found.</returns>
    Task<Guard?> GetByKeyAsync(Guid key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates a guard.
    /// </summary>
    /// <param name="guard">The guard to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The saved guard.</returns>
    Task<Guard> SaveAsync(Guard guard, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a guard.
    /// </summary>
    /// <param name="key">The guard's key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if a guard was deleted; otherwise <c>false</c>.</returns>
    Task<bool> DeleteAsync(Guid key, CancellationToken cancellationToken = default);
}
