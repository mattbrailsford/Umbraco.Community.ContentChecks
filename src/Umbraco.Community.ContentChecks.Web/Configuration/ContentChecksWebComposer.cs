using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Community.ContentChecks.Extensions;

namespace Umbraco.Community.ContentChecks.Web.Configuration;

/// <summary>
/// Composer that registers the Content Checks Management API into the Umbraco builder.
/// </summary>
public class ContentChecksWebComposer : IComposer
{
    /// <inheritdoc />
    public void Compose(IUmbracoBuilder builder) => builder.AddContentChecksWeb();
}
