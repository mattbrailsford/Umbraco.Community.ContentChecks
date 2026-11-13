using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Community.ContentChecks.AI.Tools;

namespace Umbraco.Community.ContentChecks.AI.Configuration;

/// <summary>
/// Extension methods for <see cref="IUmbracoBuilder"/> for Content Checks AI registration.
/// </summary>
public static class ContentChecksAIBuilderExtensions
{
    /// <summary>
    /// Registers Content Checks' AI-judged check and agent-tool support services. The check type
    /// itself (<c>[ContentCheck]</c>) and the tools (<c>[AITool]</c>) need no registration here --
    /// both are auto-discovered by their own owning package's collection builder (Content Checks'
    /// <c>ContentCheckCollectionBuilder</c>, Umbraco.AI's <c>AIToolCollectionBuilder</c>) the
    /// moment this assembly is loaded.
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    /// <returns>The builder, for chaining.</returns>
    public static IUmbracoBuilder AddContentChecksAI(this IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<IContentCheckAgentRoundTracker, InMemoryContentCheckAgentRoundTracker>();

        return builder;
    }
}
