namespace Umbraco.Community.ContentChecks.Web.Api.Management.Tree.Models;

/// <summary>
/// One node in the combined policy/folder tree — a folder or a policy, sibling nodes at whatever
/// level they're rendered.
/// </summary>
public sealed class TreeItemResponseModel
{
    /// <summary>
    /// The item's durable identity (a folder's or a policy's Key).
    /// </summary>
    public required Guid Key { get; init; }

    /// <summary>
    /// The display name (a folder's Name, or a policy's Name).
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Whether this node is a folder (<c>true</c>) or a policy (<c>false</c>).
    /// </summary>
    public required bool IsFolder { get; init; }

    /// <summary>
    /// Whether this node has children — always <c>false</c> for a policy; for a folder, whether
    /// it has at least one policy or subfolder directly inside it.
    /// </summary>
    public required bool HasChildren { get; init; }

    /// <summary>
    /// The containing folder. <c>null</c> means this node is at the root.
    /// </summary>
    public Guid? ParentFolderKey { get; init; }
}
