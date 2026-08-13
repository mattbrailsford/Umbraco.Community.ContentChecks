using Umbraco.Community.ContentChecks.Core.Checks;

namespace Umbraco.Community.ContentChecks.Core.Policies.Elements;

/// <summary>
/// An <see cref="IContentCheckSnapshot"/> adapter over a discovered <see cref="ContentCheckElementInstance"/>
/// (one nested element, from any registered <see cref="IContentCheckElementSource"/> — not only
/// the built-in Block List/Grid sources). <see cref="VersionId"/> is the *root* content item's own
/// version, since a nested element has no persisted version of its own — reported for provenance,
/// never used for freshness (see <see cref="ContentStamp"/>).
/// </summary>
internal sealed class ContentCheckElementInstanceSnapshot : IContentCheckSnapshot
{
    private readonly ContentCheckElementInstance _instance;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentCheckElementInstanceSnapshot"/> class.
    /// </summary>
    /// <param name="instance">The discovered element instance to wrap.</param>
    /// <param name="rootVersionId">The root content item's own version id, for reporting.</param>
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
    /// <remarks>
    /// Hashes this element's own values rather than inheriting the root document's stamp. Both
    /// would be correct — the root's stamp moves whenever any block changes — but inheriting it
    /// invalidates every block on the page whenever any one of them is edited. Stamping per element
    /// means a page of twenty Testimonials re-evaluates only the one that actually changed, which
    /// matters most for exactly the case it's most expensive to get wrong: a Metered rule on a
    /// component that appears many times (see the overview's cascading-cost open question).
    /// </remarks>
    public string? ContentStamp => ContentCheckValueStamp.Compute(
        _instance.Values.Select(v => (v.Alias, v.Culture, v.Segment, v.Value)));

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
