using System.Reflection;

namespace Umbraco.Community.ContentChecks.Core.Policies.Elements;

/// <summary>
/// What you actually write an element source by inheriting — reads
/// <see cref="PropertyEditorAlias"/> from the <see cref="ContentCheckElementSourceAttribute"/> once,
/// the same declare-once-read-via-reflection shape checks use for their own attribute.
/// </summary>
public abstract class ContentCheckElementSourceBase : IContentCheckElementSource
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentCheckElementSourceBase"/> class.
    /// </summary>
    protected ContentCheckElementSourceBase()
    {
        var attribute = GetType().GetCustomAttribute<ContentCheckElementSourceAttribute>()
            ?? throw new InvalidOperationException(
                $"{GetType()} must be decorated with [{nameof(ContentCheckElementSourceAttribute)}].");
        PropertyEditorAlias = attribute.PropertyEditorAlias;
    }

    /// <inheritdoc />
    public string PropertyEditorAlias { get; }

    /// <inheritdoc />
    public abstract IEnumerable<Guid> GetAllowedElementTypeKeys(object? dataTypeConfiguration);

    /// <inheritdoc />
    public abstract IEnumerable<ContentCheckElementInstance> GetElementInstances(object? rawValue);
}
