#pragma warning disable CS0618 // IPreviewTokenGenerator is obsolete -- see PreviewTokenCredentialMinter.
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Preview;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;
using Umbraco.Community.ContentChecks.Core.Markup;
using Umbraco.Community.ContentChecks.Extensions;

namespace Umbraco.Community.ContentChecks.Tests.Integration.Markup;

/// <summary>
/// Empirical proof for docs/markup-checks-exploration.md §9.4: that a token minted entirely by
/// code, with no real browser session or interactive editor, is one Umbraco's own preview
/// machinery actually accepts as valid. Runs against a real, EF-backed <see cref="IUserService"/>-
/// seeded super user and the real <see cref="IPreviewTokenGenerator"/> -- not mocks standing in
/// for either.
/// </summary>
[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public class PreviewTokenCredentialMinterIntegrationTests : UmbracoIntegrationTest
{
    private IContentCheckPreviewCredentialMinter Minter => GetRequiredService<IContentCheckPreviewCredentialMinter>();

    private IPreviewTokenGenerator TokenGenerator => GetRequiredService<IPreviewTokenGenerator>();

    protected override void CustomTestSetup(IUmbracoBuilder builder) => builder.AddContentChecks();

    // Umbraco.Cms.Tests.Integration's own GlobalSetupTeardown lives in that NuGet package's own
    // assembly, so it never runs for this one -- see GlobalSetupTeardown.cs in this project.
    protected override void SetUpTestConfiguration(IConfigurationBuilder configBuilder)
    {
        base.SetUpTestConfiguration(configBuilder);
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Tests:Database:DatabaseType"] = "Sqlite",
            ["Tests:Database:PrepareThreadCount"] = "1",
            ["Tests:Database:SchemaDatabaseCount"] = "1",
            ["Tests:Database:EmptyDatabasesCount"] = "1",
        });
    }

    [Test]
    public async Task MintAsync_ForTheRealSuperUser_ProducesATokenUmbracosOwnVerifierAccepts()
    {
        var credential = await Minter.MintAsync(Constants.Security.SuperUserKey, CancellationToken.None);

        Assert.That(credential, Is.Not.Null, "Minting should succeed for the seeded super user.");
        Assert.That(credential!.Cookies, Does.ContainKey(PreviewTokenCredentialMinter.PreviewCookieName));

        var token = credential.Cookies[PreviewTokenCredentialMinter.PreviewCookieName];

        // This is the real round-trip: hand the minted value to Umbraco's own verifier, exactly as
        // PreviewAuthenticationMiddleware does for an inbound request carrying the UMB_PREVIEW
        // cookie, and confirm it resolves back to the same user -- no real login, no browser.
        var verifyAttempt = await TokenGenerator.VerifyAsync(token);

        Assert.That(verifyAttempt.Success, Is.True, "Umbraco's own verifier should accept a token this component minted.");
        Assert.That(verifyAttempt.Result, Is.EqualTo(Constants.Security.SuperUserKey));
    }

    [Test]
    public async Task MintAsync_ForAUserKeyThatDoesNotExist_ReturnsNull()
    {
        var credential = await Minter.MintAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.That(credential, Is.Null);
    }
}
