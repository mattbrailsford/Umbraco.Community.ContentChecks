using System.Net;
using Moq;
using Moq.Protected;
using NUnit.Framework;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Web;
using Umbraco.Community.ContentChecks.Core.Markup;

namespace Umbraco.Community.ContentChecks.Tests.Unit.Markup;

[TestFixture]
public class HttpContentCheckMarkupProviderTests
{
    private const string ResolvedUrl = "https://example.com/some-page/";

    private Mock<IPublishedUrlProvider> _urlProviderMock = null!;
    private Mock<IUmbracoContextFactory> _umbracoContextFactoryMock = null!;
    private Mock<IContentCheckPreviewCredentialMinter> _credentialMinterMock = null!;
    private Mock<HttpMessageHandler> _httpMessageHandlerMock = null!;
    private HttpContentCheckMarkupProvider _provider = null!;

    [SetUp]
    public void SetUp()
    {
        _urlProviderMock = new Mock<IPublishedUrlProvider>();
        _urlProviderMock
            .Setup(p => p.GetUrl(It.IsAny<Guid>(), It.IsAny<UrlMode>(), It.IsAny<string?>(), It.IsAny<Uri?>()))
            .Returns(ResolvedUrl);

        _umbracoContextFactoryMock = new Mock<IUmbracoContextFactory>();
        _umbracoContextFactoryMock
            .Setup(f => f.EnsureUmbracoContext())
            .Returns((UmbracoContextReference)null!);

        _credentialMinterMock = new Mock<IContentCheckPreviewCredentialMinter>();
        _credentialMinterMock
            .Setup(m => m.MintAsync(Constants.Security.SuperUserKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContentCheckPreviewCredential { Cookies = new Dictionary<string, string> { ["UMB_PREVIEW"] = "a-token" } });

        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();

        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock
            .Setup(f => f.CreateClient(HttpContentCheckMarkupProvider.HttpClientName))
            .Returns(() => new HttpClient(_httpMessageHandlerMock.Object));

        _provider = new HttpContentCheckMarkupProvider(
            _urlProviderMock.Object,
            _umbracoContextFactoryMock.Object,
            _credentialMinterMock.Object,
            httpClientFactoryMock.Object);
    }

    private void SetUpHttpResponse(HttpStatusCode statusCode, string content = "")
        => _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode) { Content = new StringContent(content) });

    [Test]
    public async Task GetMarkupAsync_UrlProviderReturnsUnroutable_ReturnsFailureWithoutFetching()
    {
        _urlProviderMock
            .Setup(p => p.GetUrl(It.IsAny<Guid>(), It.IsAny<UrlMode>(), It.IsAny<string?>(), It.IsAny<Uri?>()))
            .Returns(Constants.Routing.Unroutable);

        var result = await _provider.GetMarkupAsync(new ContentCheckMarkupRequest { ContentKey = Guid.NewGuid() }, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.FailureReason, Does.Contain("resolve a URL"));
        _credentialMinterMock.Verify(m => m.MintAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task GetMarkupAsync_CredentialMinterReturnsNull_ReturnsFailureWithoutFetching()
    {
        _credentialMinterMock
            .Setup(m => m.MintAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ContentCheckPreviewCredential?)null);

        var result = await _provider.GetMarkupAsync(new ContentCheckMarkupRequest { ContentKey = Guid.NewGuid() }, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Url, Is.EqualTo(ResolvedUrl));
        Assert.That(result.FailureReason, Does.Contain("preview credential"));
        _httpMessageHandlerMock.Protected().Verify("SendAsync", Times.Never(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }

    [Test]
    public async Task GetMarkupAsync_SuccessfulFetch_ReturnsHtmlAndAttachesThePreviewCookie()
    {
        SetUpHttpResponse(HttpStatusCode.OK, "<html>draft content</html>");
        HttpRequestMessage? capturedRequest = null;
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("<html>draft content</html>") });

        var result = await _provider.GetMarkupAsync(new ContentCheckMarkupRequest { ContentKey = Guid.NewGuid() }, CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Html, Is.EqualTo("<html>draft content</html>"));
        Assert.That(result.StatusCode, Is.EqualTo(200));
        Assert.That(capturedRequest!.Headers.GetValues("Cookie").Single(), Is.EqualTo("UMB_PREVIEW=a-token"));
    }

    [Test]
    public async Task GetMarkupAsync_NonSuccessStatusCode_ReturnsFailureWithStatusCode()
    {
        SetUpHttpResponse(HttpStatusCode.NotFound);

        var result = await _provider.GetMarkupAsync(new ContentCheckMarkupRequest { ContentKey = Guid.NewGuid() }, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.StatusCode, Is.EqualTo(404));
        Assert.That(result.Html, Is.Null);
    }

    [Test]
    public async Task GetMarkupAsync_FetchThrows_ReturnsFailureNotAnException()
    {
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("connection refused"));

        var result = await _provider.GetMarkupAsync(new ContentCheckMarkupRequest { ContentKey = Guid.NewGuid() }, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.FailureReason, Does.Contain("connection refused"));
    }

    [Test]
    public void GetMarkupAsync_FetchCancelled_PropagatesCancellationRatherThanReportingAFailure()
    {
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        Assert.ThrowsAsync<OperationCanceledException>(() =>
            _provider.GetMarkupAsync(new ContentCheckMarkupRequest { ContentKey = Guid.NewGuid() }, CancellationToken.None));
    }
}
