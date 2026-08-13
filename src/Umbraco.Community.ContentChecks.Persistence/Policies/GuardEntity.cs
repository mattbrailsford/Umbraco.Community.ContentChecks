namespace Umbraco.Community.ContentChecks.Persistence.Policies;

/// <summary>
/// EF Core-mapped counterpart of <see cref="Core.Policies.Guard"/>. Rules are stored
/// as a JSON column rather than a child table — they have no identity or lifecycle of their own
/// and are always read/written together with their guard.
/// </summary>
internal class GuardEntity
{
    /// <summary>
    /// The guard's durable identity.
    /// </summary>
    public Guid Key { get; set; }

    /// <summary>
    /// The display name of the guard.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Restricts the guard to content underneath this node.
    /// </summary>
    public Guid? StartNodeKey { get; set; }

    /// <summary>
    /// The guard's rules, serialized as JSON.
    /// </summary>
    public string RulesJson { get; set; } = "[]";
}
