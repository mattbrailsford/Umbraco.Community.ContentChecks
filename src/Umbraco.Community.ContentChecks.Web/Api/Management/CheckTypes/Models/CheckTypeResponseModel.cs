using System.ComponentModel.DataAnnotations;
using Umbraco.Community.ContentChecks.Core.Checks;

namespace Umbraco.Community.ContentChecks.Web.Api.Management.CheckTypes.Models;

/// <summary>
/// One code-registered check type, as returned by the Management API — what the policy editor's
/// rule list picks from.
/// </summary>
public class CheckTypeResponseModel
{
    /// <summary>
    /// The check's unique, code-registered alias.
    /// </summary>
    [Required]
    public string Alias { get; set; } = string.Empty;

    /// <summary>
    /// The check's display name.
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// A description of what the check evaluates.
    /// </summary>
    [Required]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Whether running the check is metered.
    /// </summary>
    public ContentCheckCost Cost { get; set; }

    /// <summary>
    /// Whether the check's output is reproducible.
    /// </summary>
    public ContentCheckKind Kind { get; set; }

    /// <summary>
    /// The check's config schema, or <c>null</c> if it takes no configuration.
    /// </summary>
    public CheckConfigSchemaResponseModel? ConfigSchema { get; set; }
}
