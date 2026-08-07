using Umbraco.Community.ContentChecks.Core.Policies;

namespace Umbraco.Community.ContentChecks.Web.Api.Management.Folders.Models;

/// <summary>
/// Direct field-copy mapping between <see cref="ContentCheckPolicyFolder"/> and its API models —
/// same reasoning as <c>PolicyModelMappingExtensions</c>.
/// </summary>
public static class FolderModelMappingExtensions
{
    /// <summary>
    /// Maps a domain folder to its response model.
    /// </summary>
    public static FolderResponseModel ToResponseModel(this ContentCheckPolicyFolder folder) => new()
    {
        Key = folder.Key,
        Name = folder.Name,
        ParentFolderKey = folder.ParentFolderKey,
    };

    /// <summary>
    /// Maps a request model to a domain folder, using <paramref name="key"/> as its Key — the
    /// caller decides whether that comes from the request body (create) or the route (update).
    /// </summary>
    public static ContentCheckPolicyFolder ToDomain(this FolderRequestModel request, Guid key) => new()
    {
        Key = key,
        Name = request.Name,
        ParentFolderKey = request.ParentFolderKey,
    };
}
