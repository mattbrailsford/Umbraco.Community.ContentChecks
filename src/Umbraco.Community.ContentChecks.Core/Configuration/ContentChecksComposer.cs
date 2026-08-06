using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Community.ContentChecks.Extensions;

namespace Umbraco.Community.ContentChecks.Core.Configuration;

/// <summary>
/// Composer that registers Content Checks' services into the Umbraco builder.
/// </summary>
public class ContentChecksComposer : IComposer
{
    /// <inheritdoc />
    public void Compose(IUmbracoBuilder builder)
    {
        builder.AddContentChecks();
    }
}
