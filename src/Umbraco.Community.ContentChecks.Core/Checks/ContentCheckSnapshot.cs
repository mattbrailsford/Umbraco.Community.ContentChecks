using System.Globalization;
using Umbraco.Cms.Core.Models;

namespace Umbraco.Community.ContentChecks.Core.Checks;

/// <summary>
/// An <see cref="IContentCheckSnapshot"/> adapter over a real <see cref="IContent"/>. A one-line
/// delegation, since <see cref="IContentBase.GetValue{TValue}"/>/<see cref="IContentBase.HasProperty"/>
/// already have the exact shape this interface mirrors.
/// </summary>
public sealed class ContentCheckSnapshot : IContentCheckSnapshot
{
    private readonly IContent _content;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentCheckSnapshot"/> class.
    /// </summary>
    /// <param name="content">The content item to wrap.</param>
    public ContentCheckSnapshot(IContent content)
        => _content = content;

    /// <inheritdoc />
    public Guid Key => _content.Key;

    /// <inheritdoc />
    public string ContentTypeAlias => _content.ContentType.Alias;

    /// <inheritdoc />
    public int? VersionId => _content.VersionId;

    /// <inheritdoc />
    /// <remarks>
    /// Stamps from <see cref="IContentBase.UpdateDate"/>, which moves on every save — unlike
    /// <see cref="IContent.VersionId"/>, which doesn't (Umbraco updates a draft in place and only
    /// versions on publish). Deliberately not hashed and deliberately not combined with the version
    /// id: hashing a value that already discriminates exactly can only add collisions, and folding
    /// in the version id would force a needless re-evaluation of unchanged content every time a
    /// draft is published.
    /// </remarks>
    public string? ContentStamp => _content.UpdateDate.Ticks.ToString(CultureInfo.InvariantCulture);

    /// <inheritdoc />
    public bool HasProperty(string alias) => _content.HasProperty(alias);

    /// <inheritdoc />
    public TValue? GetValue<TValue>(string alias, string? culture = null, string? segment = null)
        => _content.GetValue<TValue>(alias, culture, segment);
}
