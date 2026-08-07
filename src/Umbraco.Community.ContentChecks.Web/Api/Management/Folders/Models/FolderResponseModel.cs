namespace Umbraco.Community.ContentChecks.Web.Api.Management.Folders.Models;

/// <summary>
/// A policy folder, as returned by the Management API.
/// </summary>
public sealed class FolderResponseModel
{
    /// <summary>
    /// The folder's durable identity.
    /// </summary>
    public required Guid Key { get; init; }

    /// <summary>
    /// The display name of the folder.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The containing folder. <c>null</c> means this folder is at the root.
    /// </summary>
    public Guid? ParentFolderKey { get; init; }
}
