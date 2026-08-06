namespace Umbraco.Community.ContentChecks.Persistence.Policies;

/// <summary>
/// EF Core-mapped counterpart of <see cref="Core.Policies.ContentCheckPolicy"/>. Rules are stored
/// as a JSON column rather than a child table — they have no identity or lifecycle of their own
/// and are always read/written together with their policy.
/// </summary>
internal class ContentCheckPolicyEntity
{
    /// <summary>
    /// The policy's durable identity.
    /// </summary>
    public Guid Key { get; set; }

    /// <summary>
    /// The unique alias of the policy.
    /// </summary>
    public string Alias { get; set; } = string.Empty;

    /// <summary>
    /// The display name of the policy.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The content type this policy attaches to.
    /// </summary>
    public Guid ContentTypeKey { get; set; }

    /// <summary>
    /// Restricts the policy to content underneath this node.
    /// </summary>
    public Guid? StartNodeKey { get; set; }

    /// <summary>
    /// The folder this policy is organized under in the backoffice tree.
    /// </summary>
    public Guid? ParentFolderKey { get; set; }

    /// <summary>
    /// The policy's rules, serialized as JSON.
    /// </summary>
    public string RulesJson { get; set; } = "[]";
}
