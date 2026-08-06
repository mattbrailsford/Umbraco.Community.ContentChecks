namespace Umbraco.Community.ContentChecks.Core.Config;

/// <summary>
/// Attribute to decorate a check's configuration properties with metadata for UI rendering.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class ContentCheckFieldAttribute : Attribute
{
    /// <summary>
    /// Display label for the field.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Description text for the field.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Umbraco editor UI alias for rendering the field.
    /// </summary>
    public string? EditorUiAlias { get; set; }

    /// <summary>
    /// The configuration for the editor used for the field, as a JSON string.
    /// </summary>
    public string? EditorConfig { get; set; }

    /// <summary>
    /// Sort order for displaying fields in the UI.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Optional group name used to visually group related fields in the UI. Fields with the
    /// same group name are rendered together in a separate section.
    /// </summary>
    public string? Group { get; set; }
}
