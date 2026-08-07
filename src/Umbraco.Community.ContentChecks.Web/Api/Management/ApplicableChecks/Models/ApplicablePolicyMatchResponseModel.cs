using System.ComponentModel.DataAnnotations;
using Umbraco.Community.ContentChecks.Core.Policies;

namespace Umbraco.Community.ContentChecks.Web.Api.Management.ApplicableChecks.Models;

/// <summary>
/// One policy that matched a real content item, with its provenance and rules — the pending-row
/// data an Applicable Checks panel groups by, before anything has run.
/// </summary>
public class ApplicablePolicyMatchResponseModel
{
    /// <summary>
    /// The matching policy's key.
    /// </summary>
    [Required]
    public Guid PolicyKey { get; set; }

    /// <summary>
    /// The matching policy's alias.
    /// </summary>
    [Required]
    public string PolicyAlias { get; set; } = string.Empty;

    /// <summary>
    /// The matching policy's display name.
    /// </summary>
    [Required]
    public string PolicyName { get; set; } = string.Empty;

    /// <summary>
    /// How this policy came to match — Direct, Composition, or Cascading. For Composition, the
    /// "via" type is the policy's own content type (it targets the type this item composes
    /// from); for Cascading, the "via" property/element is the containing
    /// <see cref="ApplicableCheckTargetResponseModel"/>'s own <c>Path</c> and
    /// <c>ContentTypeAlias</c> — nothing extra needed here.
    /// </summary>
    public ContentCheckEffectiveCheckProvenance Provenance { get; set; }

    /// <summary>
    /// This policy's rules that resolved to a registered check.
    /// </summary>
    public IReadOnlyList<ApplicableRuleResponseModel> Rules { get; set; } = [];
}
