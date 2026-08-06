namespace Umbraco.Community.ContentChecks.Core.Policies.Elements;

/// <summary>
/// One element-type instance found inside a property's stored value (e.g. one block in a Block
/// List/Grid), as discovered by an <see cref="IContentCheckElementSource"/> at runtime.
/// </summary>
public sealed class ContentCheckElementInstance
{
    /// <summary>
    /// The instance's own durable identity (e.g. the block's key).
    /// </summary>
    public required Guid Key { get; init; }

    /// <summary>
    /// The Key of the element type this instance is of.
    /// </summary>
    public required Guid ContentTypeKey { get; init; }

    /// <summary>
    /// The alias of the element type this instance is of.
    /// </summary>
    public required string ContentTypeAlias { get; init; }

    /// <summary>
    /// The instance's own property values.
    /// </summary>
    public required IReadOnlyList<ContentCheckElementPropertyValue> Values { get; init; }
}
