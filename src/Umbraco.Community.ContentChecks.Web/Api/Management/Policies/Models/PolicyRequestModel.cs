using System.ComponentModel.DataAnnotations;

namespace Umbraco.Community.ContentChecks.Web.Api.Management.Policies.Models;

/// <summary>
/// A check policy to create or update. Shared between create and update: create takes the
/// <c>Key</c> from this body (the client generates it, the same convention Umbraco's own backoffice
/// uses for new entities); update takes the <c>Key</c> from the route instead and ignores this one.
/// </summary>
public class PolicyRequestModel
{
    /// <summary>
    /// The policy's durable identity. Only read on create; a route key always wins on update.
    /// </summary>
    [Required]
    public Guid Key { get; set; }

    /// <summary>
    /// The unique alias of the policy.
    /// </summary>
    [Required]
    public string Alias { get; set; } = string.Empty;

    /// <summary>
    /// The display name of the policy.
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The content type this policy attaches to, by its durable Key.
    /// </summary>
    [Required]
    public Guid ContentTypeKey { get; set; }

    /// <summary>
    /// Restricts the policy to content underneath this node. <c>null</c> means everywhere the
    /// content type reaches.
    /// </summary>
    public Guid? StartNodeKey { get; set; }

    /// <summary>
    /// The folder this policy is organized under in the backoffice tree. <c>null</c> means at the root.
    /// </summary>
    public Guid? ParentFolderKey { get; set; }

    /// <summary>
    /// The rules this policy runs.
    /// </summary>
    public IReadOnlyList<PolicyRuleModel> Rules { get; set; } = [];
}
