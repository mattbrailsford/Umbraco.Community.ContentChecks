namespace Umbraco.Community.ContentChecks.Persistence.Policies;

/// <summary>
/// EF Core-mapped counterpart of <see cref="Core.Policies.ContentCheckPolicyFolder"/>.
/// </summary>
internal class ContentCheckPolicyFolderEntity
{
    /// <summary>
    /// The folder's durable identity.
    /// </summary>
    public Guid Key { get; set; }

    /// <summary>
    /// The display name of the folder.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The containing folder. <c>null</c> means this folder is at the root.
    /// </summary>
    public Guid? ParentFolderKey { get; set; }
}
