using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Umbraco.Community.ContentChecks.Core.Config;

/// <summary>
/// A single field definition within a <see cref="ContentCheckConfigSchema"/>.
/// </summary>
public class ContentCheckField
{
    /// <summary>
    /// The unique key identifying the field within its schema.
    /// </summary>
    public required string Key { get; set; }

    /// <summary>
    /// The label for the field.
    /// </summary>
    public required string Label { get; set; }

    /// <summary>
    /// The description of the field.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The name of the property this field was built from.
    /// </summary>
    [JsonIgnore]
    public string? PropertyName { get; set; }

    /// <summary>
    /// The type of the property this field was built from.
    /// </summary>
    [JsonIgnore]
    public Type? PropertyType { get; set; }

    /// <summary>
    /// The UI alias of the editor used for the field.
    /// </summary>
    public string? EditorUiAlias { get; set; }

    /// <summary>
    /// The configuration for the editor used for the field.
    /// </summary>
    public object? EditorConfig { get; set; }

    /// <summary>
    /// The default value of the field from the config class's property initializer.
    /// </summary>
    public object? DefaultValue { get; set; }

    /// <summary>
    /// The sort order of the field in the UI.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// The validation rules applied to the field.
    /// </summary>
    public IEnumerable<ValidationAttribute> ValidationRules { get; set; } = [];

    /// <summary>
    /// Optional group identifier for visually grouping related fields in the UI.
    /// </summary>
    public string? Group { get; set; }
}
