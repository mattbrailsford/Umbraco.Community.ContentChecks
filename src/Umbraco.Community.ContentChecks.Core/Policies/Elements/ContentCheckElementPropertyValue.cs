namespace Umbraco.Community.ContentChecks.Core.Policies.Elements;

/// <summary>
/// One property value on a discovered <see cref="ContentCheckElementInstance"/>.
/// </summary>
public sealed class ContentCheckElementPropertyValue
{
    /// <summary>
    /// The property alias.
    /// </summary>
    public required string Alias { get; init; }

    /// <summary>
    /// The culture this value is for, or <c>null</c> for an invariant property.
    /// </summary>
    public string? Culture { get; init; }

    /// <summary>
    /// The segment this value is for, or <c>null</c> for an unsegmented property.
    /// </summary>
    public string? Segment { get; init; }

    /// <summary>
    /// The stored value.
    /// </summary>
    public object? Value { get; init; }

    /// <summary>
    /// The alias of the property editor this value belongs to — how the resolution layer knows
    /// whether to recurse into this value with another <see cref="IContentCheckElementSource"/>.
    /// </summary>
    public string? PropertyEditorAlias { get; init; }
}
