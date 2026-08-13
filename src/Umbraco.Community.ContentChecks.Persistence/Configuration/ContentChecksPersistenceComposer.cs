using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Community.ContentChecks.Core.Configuration;
using Umbraco.Community.ContentChecks.Extensions;

namespace Umbraco.Community.ContentChecks.Persistence.Configuration;

/// <summary>
/// Composer that registers Content Checks' EF Core persistence into the Umbraco builder,
/// overriding the in-memory defaults registered by <see cref="ContentChecksComposer"/> in Core.
/// Explicit <see cref="ComposeAfterAttribute"/> is required for that override to actually happen —
/// composers with no ordering directive run in an order this package can't rely on, and the last
/// registration of a given service wins; without this, whichever composer happens to run last on a
/// given host decides whether persistence is EF Core-backed or silently falls back to in-memory,
/// with no error either way. Confirmed the hard way: without it, every "save" appeared to succeed
/// but nothing ever reached the database.
/// </summary>
[ComposeAfter(typeof(ContentChecksComposer))]
public class ContentChecksPersistenceComposer : IComposer
{
    /// <inheritdoc />
    public void Compose(IUmbracoBuilder builder) => builder.AddContentChecksPersistence();
}
