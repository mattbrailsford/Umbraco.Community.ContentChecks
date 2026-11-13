using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.Community.ContentChecks.AI.Configuration;

/// <summary>
/// Composer that registers Content Checks' AI-judged check and agent tools into the Umbraco
/// builder. Ordering relative to Content Checks' own composer and Umbraco.AI's own composer
/// doesn't matter: each only registers services and sets up its own attribute-driven
/// auto-discovery ([ContentCheck], [AITool]), which runs once every composer has had a chance to
/// contribute, not at composition time.
/// </summary>
public class ContentChecksAIComposer : IComposer
{
    /// <inheritdoc />
    public void Compose(IUmbracoBuilder builder) => builder.AddContentChecksAI();
}
