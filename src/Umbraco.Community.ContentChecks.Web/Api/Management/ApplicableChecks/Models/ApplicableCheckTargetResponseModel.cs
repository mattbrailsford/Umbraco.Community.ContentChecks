using System.ComponentModel.DataAnnotations;
using Umbraco.Community.ContentChecks.Core.Policies;

namespace Umbraco.Community.ContentChecks.Web.Api.Management.ApplicableChecks.Models;

/// <summary>
/// One resolved target for a real content item — the root item itself, or one nested element
/// found by cascading — with every policy that matched it. This is resolution's own accurate,
/// per-item picture (Policy Resolution spec §7): nothing here can suffer the "shows up but
/// doesn't really apply" problem a schema-level view would have.
/// </summary>
public class ApplicableCheckTargetResponseModel
{
    /// <summary>
    /// The content type alias of the snapshot this target represents — the root item's own type,
    /// or a nested element's type for a cascading target.
    /// </summary>
    [Required]
    public string ContentTypeAlias { get; set; } = string.Empty;

    /// <summary>
    /// The root-to-here chain of nested elements this target was discovered through. Empty for
    /// the root item's own target.
    /// </summary>
    public IReadOnlyList<ContentCheckLocationStep> Path { get; set; } = [];

    /// <summary>
    /// Every policy that matched this target.
    /// </summary>
    public IReadOnlyList<ApplicablePolicyMatchResponseModel> PolicyMatches { get; set; } = [];
}
