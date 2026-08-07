using System.ComponentModel.DataAnnotations;

namespace Umbraco.Community.ContentChecks.Web.Api.Management.Folders.Models;

/// <summary>
/// A policy folder, as returned by the Management API.
/// </summary>
public class FolderResponseModel
{
    /// <summary>
    /// The folder's durable identity.
    /// </summary>
    [Required]
    public Guid Key { get; set; }

    /// <summary>
    /// The display name of the folder.
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The containing folder. <c>null</c> means this folder is at the root.
    /// </summary>
    public Guid? ParentFolderKey { get; set; }
}
