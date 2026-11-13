#pragma warning disable CS0618 // IPreviewTokenGenerator is obsolete -- see PreviewTokenCredentialMinter.
using Moq;
using NUnit.Framework;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Preview;
using Umbraco.Community.ContentChecks.Core.Markup;

namespace Umbraco.Community.ContentChecks.Tests.Unit.Markup;

[TestFixture]
public class PreviewTokenCredentialMinterTests
{
    private Mock<IPreviewTokenGenerator> _tokenGeneratorMock = null!;
    private PreviewTokenCredentialMinter _minter = null!;

    [SetUp]
    public void SetUp()
    {
        _tokenGeneratorMock = new Mock<IPreviewTokenGenerator>();
        _minter = new PreviewTokenCredentialMinter(_tokenGeneratorMock.Object);
    }

    [Test]
    public async Task MintAsync_TokenGeneratorSucceeds_ReturnsCredentialWithThePreviewCookie()
    {
        var userKey = Guid.NewGuid();
        _tokenGeneratorMock
            .Setup(g => g.GenerateTokenAsync(userKey))
            .ReturnsAsync(Attempt.Succeed<string?>("the-minted-token"));

        var credential = await _minter.MintAsync(userKey, CancellationToken.None);

        Assert.That(credential, Is.Not.Null);
        Assert.That(credential!.Cookies, Has.Count.EqualTo(1));
        Assert.That(credential.Cookies[PreviewTokenCredentialMinter.PreviewCookieName], Is.EqualTo("the-minted-token"));
    }

    [Test]
    public async Task MintAsync_TokenGeneratorFails_ReturnsNull()
    {
        var userKey = Guid.NewGuid();
        _tokenGeneratorMock
            .Setup(g => g.GenerateTokenAsync(userKey))
            .ReturnsAsync(Attempt<string?>.Fail());

        var credential = await _minter.MintAsync(userKey, CancellationToken.None);

        Assert.That(credential, Is.Null);
    }
}
