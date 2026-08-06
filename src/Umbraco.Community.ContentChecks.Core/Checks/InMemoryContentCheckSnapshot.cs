namespace Umbraco.Community.ContentChecks.Core.Checks;

/// <summary>
/// An <see cref="IContentCheckSnapshot"/> backed by an in-memory dictionary rather than a real
/// <see cref="Umbraco.Cms.Core.Models.IContent"/>. Represents content with no persisted version
/// yet — an unsaved editor draft, a Draft Set's pending state — which is exactly the case
/// <see cref="IContentCheckSnapshot.VersionId"/> being <c>null</c> exists for. Also the natural
/// test double for exercising checks without a real Umbraco content item.
/// </summary>
public sealed class InMemoryContentCheckSnapshot : IContentCheckSnapshot
{
    private readonly Dictionary<(string Alias, string? Culture, string? Segment), object?> _values = [];
    private readonly HashSet<string> _declaredProperties = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryContentCheckSnapshot"/> class.
    /// </summary>
    /// <param name="key">The content item's durable identity.</param>
    /// <param name="contentTypeAlias">The content type alias.</param>
    public InMemoryContentCheckSnapshot(Guid key, string contentTypeAlias)
    {
        Key = key;
        ContentTypeAlias = contentTypeAlias;
    }

    /// <inheritdoc />
    public Guid Key { get; }

    /// <inheritdoc />
    public string ContentTypeAlias { get; }

    /// <inheritdoc />
    public int? VersionId => null;

    /// <inheritdoc />
    public bool HasProperty(string alias) => _declaredProperties.Contains(alias);

    /// <inheritdoc />
    public TValue? GetValue<TValue>(string alias, string? culture = null, string? segment = null)
        => _values.TryGetValue((alias, culture, segment), out var value) && value is TValue typed
            ? typed
            : default;

    /// <summary>
    /// Declares a property and sets its value for the given variant.
    /// </summary>
    /// <param name="alias">The property alias.</param>
    /// <param name="value">The value to set.</param>
    /// <param name="culture">The culture this value is for, or <c>null</c> for an invariant property.</param>
    /// <param name="segment">The segment this value is for, or <c>null</c> for an unsegmented property.</param>
    /// <returns>This instance, for chaining.</returns>
    public InMemoryContentCheckSnapshot SetValue(string alias, object? value, string? culture = null, string? segment = null)
    {
        _declaredProperties.Add(alias);
        _values[(alias, culture, segment)] = value;
        return this;
    }
}
