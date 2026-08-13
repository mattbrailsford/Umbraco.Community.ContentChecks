using System.Collections.Concurrent;

namespace Umbraco.Community.ContentChecks.Core.Policies;

/// <summary>
/// The zero-configuration default <see cref="IGuardRepository"/> — keeps the package
/// usable with no persistence set up. Mirrors <see cref="InMemoryContentCheckPolicyRepository"/>'s
/// role for policies. A real persistence layer overrides this registration.
/// </summary>
internal sealed class InMemoryGuardRepository : IGuardRepository
{
    private readonly ConcurrentDictionary<Guid, Guard> _guards = new();

    /// <inheritdoc />
    public Task<IReadOnlyList<Guard>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Guard>>(_guards.Values.ToList());

    /// <inheritdoc />
    public Task<Guard?> GetByKeyAsync(Guid key, CancellationToken cancellationToken = default)
        => Task.FromResult(_guards.GetValueOrDefault(key));

    /// <inheritdoc />
    public Task<Guard> SaveAsync(Guard guard, CancellationToken cancellationToken = default)
    {
        _guards[guard.Key] = guard;
        return Task.FromResult(guard);
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(Guid key, CancellationToken cancellationToken = default)
        => Task.FromResult(_guards.TryRemove(key, out _));
}
