using System.Text.Json;
using Umbraco.Community.ContentChecks.Core.Policies;

namespace Umbraco.Community.ContentChecks.Persistence.Policies;

/// <summary>
/// Default implementation of <see cref="IGuardFactory"/>.
/// </summary>
internal sealed class GuardFactory : IGuardFactory
{
    /// <inheritdoc />
    public Guard BuildDomain(GuardEntity entity) => new()
    {
        Key = entity.Key,
        Name = entity.Name,
        StartNodeKey = entity.StartNodeKey,
        Rules = JsonSerializer.Deserialize<List<GuardRule>>(entity.RulesJson) ?? [],
    };

    /// <inheritdoc />
    public GuardEntity BuildEntity(Guard guard) => new()
    {
        Key = guard.Key,
        Name = guard.Name,
        StartNodeKey = guard.StartNodeKey,
        RulesJson = JsonSerializer.Serialize(guard.Rules),
    };

    /// <inheritdoc />
    public void UpdateEntity(GuardEntity entity, Guard guard)
    {
        entity.Name = guard.Name;
        entity.StartNodeKey = guard.StartNodeKey;
        entity.RulesJson = JsonSerializer.Serialize(guard.Rules);
    }
}
