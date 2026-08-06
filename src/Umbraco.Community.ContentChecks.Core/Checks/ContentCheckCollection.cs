using Umbraco.Cms.Core.Composing;

namespace Umbraco.Community.ContentChecks.Core.Checks;

/// <summary>
/// A collection of registered check types.
/// </summary>
public sealed class ContentCheckCollection : BuilderCollectionBase<IContentCheck>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentCheckCollection"/> class.
    /// </summary>
    /// <param name="items">A factory function that returns the check instances.</param>
    public ContentCheckCollection(Func<IEnumerable<IContentCheck>> items)
        : base(items)
    {
    }

    /// <summary>
    /// Gets a check by its alias.
    /// </summary>
    /// <param name="alias">The check alias.</param>
    /// <returns>The check, or <c>null</c> if not found.</returns>
    public IContentCheck? GetByAlias(string alias)
        => this.FirstOrDefault(c => c.Alias.Equals(alias, StringComparison.OrdinalIgnoreCase));
}
