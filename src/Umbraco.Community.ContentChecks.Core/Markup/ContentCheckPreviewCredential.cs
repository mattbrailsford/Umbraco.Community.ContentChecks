namespace Umbraco.Community.ContentChecks.Core.Markup;

/// <summary>
/// A credential that puts an outbound page-fetch request into Umbraco preview mode, as one or
/// more cookies to attach to it. Minted by <see cref="IContentCheckPreviewCredentialMinter"/>.
/// </summary>
/// <remarks>
/// A dictionary of cookies, not a single value, because the real mechanism differs by Umbraco
/// major: v18's <see cref="PreviewTokenCredentialMinter"/> needs only one (<c>UMB_PREVIEW</c>),
/// but Umbraco 19 (not yet built here -- see docs/markup-checks-exploration.md) replaces that
/// with a real backoffice sign-in, which needs both that same flag cookie and a separate auth
/// cookie.
/// </remarks>
public sealed class ContentCheckPreviewCredential
{
    /// <summary>
    /// The cookies to attach to the outbound request, keyed by cookie name.
    /// </summary>
    public required IReadOnlyDictionary<string, string> Cookies { get; init; }
}
