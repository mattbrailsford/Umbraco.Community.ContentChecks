using System.ComponentModel.DataAnnotations;

namespace Umbraco.Community.ContentChecks.Web.Api.Management.Folders.Models;

/// <summary>
/// A policy folder to create or update. Shared between create and update, same reasoning as
/// <c>PolicyRequestModel</c> — create takes <c>Key</c> from this body, update takes it from the
/// route. Renaming and moving (via <see cref="ParentFolderKey"/>) both go through this one call,
/// not separate endpoints.
/// </summary>
public class FolderRequestModel
{
    /// <summary>
    /// The folder's durable identity. Only read on create; a route key always wins on update.
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
