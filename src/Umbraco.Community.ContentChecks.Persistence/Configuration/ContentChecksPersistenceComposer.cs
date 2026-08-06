using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Community.ContentChecks.Extensions;

namespace Umbraco.Community.ContentChecks.Persistence.Configuration;

/// <summary>
/// Composer that registers Content Checks' EF Core persistence into the Umbraco builder,
/// overriding the in-memory defaults registered by <c>ContentChecksComposer</c> in Core.
/// </summary>
public class ContentChecksPersistenceComposer : IComposer
{
    /// <inheritdoc />
    public void Compose(IUmbracoBuilder builder) => builder.AddContentChecksPersistence();
}
