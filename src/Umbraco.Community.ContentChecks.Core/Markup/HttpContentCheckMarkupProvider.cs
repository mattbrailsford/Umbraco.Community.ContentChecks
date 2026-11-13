using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Web;

namespace Umbraco.Community.ContentChecks.Core.Markup;

/// <summary>
/// The default <see cref="IContentCheckMarkupProvider"/>: resolves the content's own URL and
/// fetches it over plain HTTP, attaching a minted preview credential so the fetch sees the
/// current draft rather than the published version (docs/markup-checks-exploration.md §9.3,
/// Option A -- the HTTP round-trip, not in-process rendering, which doesn't generalize to a
/// headless frontend anyway).
/// </summary>
/// <remarks>
/// <para>
/// <b>Not yet proven against a real running site</b> -- URL resolution for content (draft or
/// published) couldn't be exercised end-to-end in this project's lightweight SQLite integration-
/// test harness (the published-content/navigation cache isn't populated by a bare
/// <c>IContentService.Save</c>/<c>Publish</c> call there, even with the harness's cache-warm-up
/// options enabled). This is standard, universally-relied-on Umbraco behavior on a real site --
/// resolving a content item's own URL is how every Umbraco page gets routed at all -- but that
/// specific claim is trusted here, not test-proven in this repo. Re-verify against the real demo
/// site once there's a convenient way to create content through it end-to-end.
/// </para>
/// <para>
/// <b>The "judge" user is hard-coded to the super user for now</b>, not configurable -- see
/// <see cref="IContentCheckPreviewCredentialMinter"/>'s remarks on why an Admin-group account
/// avoids needing per-node permission reasoning. A real setting can replace this once something
/// else in the project needs user-configurable settings too; building one for just this would be
/// ahead of actual need.
/// </para>
/// </remarks>
internal sealed class HttpContentCheckMarkupProvider : IContentCheckMarkupProvider
{
    /// <summary>
    /// The name this provider's <see cref="IHttpClientFactory"/> client is registered under.
    /// </summary>
    internal const string HttpClientName = "Umbraco.Community.ContentChecks.Markup";

    private readonly IPublishedUrlProvider _urlProvider;
    private readonly IUmbracoContextFactory _umbracoContextFactory;
    private readonly IContentCheckPreviewCredentialMinter _credentialMinter;
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpContentCheckMarkupProvider"/> class.
    /// </summary>
    /// <param name="urlProvider">Resolves the content item's own URL.</param>
    /// <param name="umbracoContextFactory">Ensures an ambient <c>UmbracoContext</c> exists -- this provider runs from a background check run, not a live request.</param>
    /// <param name="credentialMinter">Mints the preview credential attached to the outbound fetch.</param>
    /// <param name="httpClientFactory">Creates the <see cref="HttpClientName"/> client used for the fetch.</param>
    public HttpContentCheckMarkupProvider(
        IPublishedUrlProvider urlProvider,
        IUmbracoContextFactory umbracoContextFactory,
        IContentCheckPreviewCredentialMinter credentialMinter,
        IHttpClientFactory httpClientFactory)
    {
        _urlProvider = urlProvider;
        _umbracoContextFactory = umbracoContextFactory;
        _credentialMinter = credentialMinter;
        _httpClientFactory = httpClientFactory;
    }

    /// <inheritdoc />
    public async Task<ContentCheckMarkupResult> GetMarkupAsync(ContentCheckMarkupRequest request, CancellationToken cancellationToken)
    {
        string url;
        using (_umbracoContextFactory.EnsureUmbracoContext())
        {
            url = _urlProvider.GetUrl(request.ContentKey, UrlMode.Absolute, request.Culture);
        }

        if (string.IsNullOrEmpty(url) || url == Constants.Routing.Unroutable)
        {
            return new ContentCheckMarkupResult
            {
                Success = false,
                FailureReason = "Could not resolve a URL for this content.",
            };
        }

        var credential = await _credentialMinter.MintAsync(Constants.Security.SuperUserKey, cancellationToken);
        if (credential is null)
        {
            return new ContentCheckMarkupResult
            {
                Success = false,
                Url = url,
                FailureReason = "Could not mint a preview credential.",
            };
        }

        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
        requestMessage.Headers.Add("Cookie", string.Join("; ", credential.Cookies.Select(c => $"{c.Key}={c.Value}")));

        var httpClient = _httpClientFactory.CreateClient(HttpClientName);

        try
        {
            using var response = await httpClient.SendAsync(requestMessage, cancellationToken);
            var html = await response.Content.ReadAsStringAsync(cancellationToken);

            return new ContentCheckMarkupResult
            {
                Success = response.IsSuccessStatusCode,
                Html = response.IsSuccessStatusCode ? html : null,
                Url = url,
                StatusCode = (int)response.StatusCode,
                FailureReason = response.IsSuccessStatusCode ? null : $"The page responded with status {(int)response.StatusCode}.",
            };
        }
        catch (OperationCanceledException)
        {
            // Mirrors RubricContentCheck -- a cancelled fetch isn't a verdict, let it propagate.
            throw;
        }
        catch (Exception ex)
        {
            return new ContentCheckMarkupResult
            {
                Success = false,
                Url = url,
                FailureReason = $"Fetching the page failed: {ex.Message}",
            };
        }
    }
}
