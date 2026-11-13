using Umbraco.AI.Core.Tools.Scopes;

namespace Umbraco.Community.ContentChecks.AI.Tools;

/// <summary>
/// The AI tool scope every Content Checks tool declares itself under, for permission and
/// grouping purposes -- auto-discovered by Umbraco.AI the same way as its own built-in scopes
/// (e.g. <c>ContentReadScope</c>).
/// </summary>
[AIToolScope(ScopeId, Icon = "icon-clipboard-check", Domain = "Content Checks")]
public sealed class ContentChecksToolScope : AIToolScopeBase
{
    /// <summary>
    /// The scope's id, referenced by every tool's <c>[AITool(..., ScopeId = ...)]</c>.
    /// </summary>
    public const string ScopeId = "content-checks";
}
