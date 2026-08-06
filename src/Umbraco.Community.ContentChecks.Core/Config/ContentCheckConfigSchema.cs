namespace Umbraco.Community.ContentChecks.Core.Config;

/// <summary>
/// The schema of a check's configuration type: its type and field definitions.
/// </summary>
public class ContentCheckConfigSchema
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentCheckConfigSchema"/> class.
    /// </summary>
    /// <param name="type">The configuration type.</param>
    /// <param name="fields">The field definitions for the type.</param>
    public ContentCheckConfigSchema(Type type, IReadOnlyList<ContentCheckField> fields)
    {
        Type = type;
        Fields = fields;
    }

    /// <summary>
    /// The configuration type this schema was built from.
    /// </summary>
    public Type Type { get; }

    /// <summary>
    /// The field definitions for the configuration type.
    /// </summary>
    public IReadOnlyList<ContentCheckField> Fields { get; }
}
