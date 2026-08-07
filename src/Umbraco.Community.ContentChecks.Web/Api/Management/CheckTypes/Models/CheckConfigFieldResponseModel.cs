using System.ComponentModel.DataAnnotations;

namespace Umbraco.Community.ContentChecks.Web.Api.Management.CheckTypes.Models;

/// <summary>
/// One field within a <see cref="CheckConfigSchemaResponseModel"/> — the UI renders every check's
/// config form from this, no per-type frontend code.
/// </summary>
public class CheckConfigFieldResponseModel
{
    /// <summary>
    /// The unique key identifying the field within its schema.
    /// </summary>
    [Required]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// The label for the field.
    /// </summary>
    [Required]
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// The description of the field.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The alias of the backoffice property editor UI used to render the field. <c>null</c>
    /// means the caller falls back to a plain text box.
    /// </summary>
    public string? EditorUiAlias { get; set; }

    /// <summary>
    /// The configuration for the editor used for the field.
    /// </summary>
    public object? EditorConfig { get; set; }

    /// <summary>
    /// The default value of the field.
    /// </summary>
    public object? DefaultValue { get; set; }

    /// <summary>
    /// The sort order of the field in the UI.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Optional group for visually grouping related fields in the UI.
    /// </summary>
    public string? Group { get; set; }

    /// <summary>
    /// Whether the field must have a value.
    /// </summary>
    public bool IsRequired { get; set; }
}
