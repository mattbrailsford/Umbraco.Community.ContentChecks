using System.ComponentModel.DataAnnotations;

namespace Umbraco.Community.ContentChecks.Web.Api.Management.Guards.Models;

/// <summary>
/// An guard, as returned by the Management API.
/// </summary>
public class GuardResponseModel
{
    /// <summary>
    /// The guard's durable identity.
    /// </summary>
    [Required]
    public Guid Key { get; set; }

    /// <summary>
    /// The display name of the guard.
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Restricts the guard to content underneath this node. <c>null</c> means the whole site.
    /// </summary>
    public Guid? StartNodeKey { get; set; }

    /// <summary>
    /// The rules this guard resolves against, most specific first.
    /// </summary>
    public IReadOnlyList<GuardRuleModel> Rules { get; set; } = [];
}
