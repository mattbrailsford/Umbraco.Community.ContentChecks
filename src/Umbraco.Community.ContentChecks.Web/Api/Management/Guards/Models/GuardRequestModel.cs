using System.ComponentModel.DataAnnotations;

namespace Umbraco.Community.ContentChecks.Web.Api.Management.Guards.Models;

/// <summary>
/// An guard to create or update. Shared between create and update: create takes
/// the <c>Key</c> from this body (the client generates it, the same convention Umbraco's own
/// backoffice uses for new entities); update takes the <c>Key</c> from the route instead and
/// ignores this one.
/// </summary>
public class GuardRequestModel
{
    /// <summary>
    /// The guard's durable identity. Only read on create; a route key always wins on update.
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
