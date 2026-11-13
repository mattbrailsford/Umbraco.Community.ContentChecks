#pragma warning disable CS0618 // IPreviewTokenGenerator is obsolete, removed entirely in Umbraco
// 19 -- see docs/markup-checks-exploration.md §9.4. This repo's branch-per-major model means
// v18/dev never upgrades past Umbraco 18, so this is safe for this branch's whole lifetime; a
// v19/dev branch needs its own IContentCheckPreviewCredentialMinter built on a real backoffice
// sign-in instead.
using Umbraco.Cms.Core.Preview;

namespace Umbraco.Community.ContentChecks.Core.Markup;

/// <summary>
/// Umbraco 18's <see cref="IContentCheckPreviewCredentialMinter"/>: wraps Umbraco's own preview
/// token generator. See the interface's remarks for why this sits behind its own seam rather
/// than being called directly from a markup provider.
/// </summary>
internal sealed class PreviewTokenCredentialMinter : IContentCheckPreviewCredentialMinter
{
    /// <summary>
    /// The cookie Umbraco's <c>PreviewAuthenticationMiddleware</c> looks for on an inbound
    /// request to decide whether to attempt preview mode at all.
    /// </summary>
    internal const string PreviewCookieName = "UMB_PREVIEW";

    private readonly IPreviewTokenGenerator _tokenGenerator;

    /// <summary>
    /// Initializes a new instance of the <see cref="PreviewTokenCredentialMinter"/> class.
    /// </summary>
    /// <param name="tokenGenerator">Mints and verifies the real <c>UMB_PREVIEW</c> token.</param>
    public PreviewTokenCredentialMinter(IPreviewTokenGenerator tokenGenerator) => _tokenGenerator = tokenGenerator;

    /// <inheritdoc />
    public async Task<ContentCheckPreviewCredential?> MintAsync(Guid judgeUserKey, CancellationToken cancellationToken)
    {
        var attempt = await _tokenGenerator.GenerateTokenAsync(judgeUserKey);

        return attempt is { Success: true, Result: { } token }
            ? new ContentCheckPreviewCredential
            {
                Cookies = new Dictionary<string, string> { [PreviewCookieName] = token },
            }
            : null;
    }
}
