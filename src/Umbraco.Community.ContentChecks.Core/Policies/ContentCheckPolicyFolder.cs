namespace Umbraco.Community.ContentChecks.Core.Policies;

/// <summary>
/// A folder for organizing check policies in the backoffice tree, the way Data Types/Document
/// Types/Media Types are — a distinct entity with a self-referencing nullable parent, no
/// cascading discriminator on the policy itself. The durable identifier and the self-reference
/// are named <c>Key</c>, matching this project's own Id/Key/Alias discipline, since a folder's
/// <see cref="Name"/> is exactly as renamable as a content type's alias.
/// </summary>
public sealed class ContentCheckPolicyFolder
{
    /// <summary>
    /// The folder's durable identity.
    /// </summary>
    public Guid Key { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The display name of the folder.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// The containing folder. <c>null</c> means this folder is at the root.
    /// </summary>
    public Guid? ParentFolderKey { get; set; }
}
