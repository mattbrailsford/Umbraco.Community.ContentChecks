using Umbraco.Community.ContentChecks.Core.Checks;

namespace Umbraco.Community.ContentChecks.Core.Policies.Elements;

/// <summary>
/// An <see cref="IContentCheckSnapshot"/> adapter over a discovered <see cref="ContentCheckElementInstance"/>
/// (one nested element, from any registered <see cref="IContentCheckElementSource"/> — not only
/// the built-in Block List/Grid sources). <see cref="VersionId"/> is the *root* content item's own
/// version, not the element's own identity — a nested element has no persisted version of its own,
/// but the whole property value it lives inside changes exactly when the root document's version
/// does, so keying the cache off the root's version id still invalidates correctly.
/// </summary>
internal sealed class ContentCheckElementInstanceSnapshot : IContentCheckSnapshot
{
    private readonly ContentCheckElementInstance _instance;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentCheckElementInstanceSnapshot"/> class.
    /// </summary>
    /// <param name="instance">The discovered element instance to wrap.</param>
    /// <param name="rootVersionId">The root content item's own version id.</param>
    public ContentCheckElementInstanceSnapshot(ContentCheckElementInstance instance, int? rootVersionId)
    {
        _instance = instance;
        VersionId = rootVersionId;
    }

    /// <inheritdoc />
    public Guid Key => _instance.Key;

    /// <inheritdoc />
    public string ContentTypeAlias => _instance.ContentTypeAlias;

    /// <inheritdoc />
    public int? VersionId { get; }

    /// <inheritdoc />
    public bool HasProperty(string alias)
        => _instance.Values.Any(v => string.Equals(v.Alias, alias, StringComparison.OrdinalIgnoreCase));

    /// <inheritdoc />
    public TValue? GetValue<TValue>(string alias, string? culture = null, string? segment = null)
    {
        var match = _instance.Values.FirstOrDefault(v =>
            string.Equals(v.Alias, alias, StringComparison.OrdinalIgnoreCase)
            && v.Culture == culture
            && v.Segment == segment);

        return match?.Value is TValue typed ? typed : default;
    }
}
