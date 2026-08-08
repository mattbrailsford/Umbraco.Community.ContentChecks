using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Tests.Common.Builders;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;
using Umbraco.Community.ContentChecks.Core.Policies;
using Umbraco.Community.ContentChecks.Extensions;

namespace Umbraco.Community.ContentChecks.Tests.Integration.Policies;

/// <summary>
/// Closes the real-<c>IContent</c>/<c>IContentType</c> gap the Moq-based unit tests (see
/// <c>ContentCheckResolutionServiceTests</c>) left open: those prove the matching logic against
/// mocked <c>IContentType.CompositionKeys()</c>/<c>IContent.GetValue</c>, which only demonstrates
/// the logic is internally consistent with an assumption about how real Umbraco behaves. These
/// tests run the same scenarios against a real EF-backed content type/content item to confirm
/// that assumption is actually correct.
/// </summary>
[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public class ContentCheckResolutionServiceIntegrationTests : UmbracoIntegrationTest
{
    private IContentTypeService ContentTypeService => GetRequiredService<IContentTypeService>();

    private IContentService ContentService => GetRequiredService<IContentService>();

    private IContentCheckPolicyService PolicyService => GetRequiredService<IContentCheckPolicyService>();

    private IContentCheckResolutionService ResolutionService => GetRequiredService<IContentCheckResolutionService>();

    protected override void CustomTestSetup(IUmbracoBuilder builder) => builder.AddContentChecks();

    // Umbraco.Cms.Tests.Integration's own GlobalSetupTeardown (which loads appsettings.Tests.json
    // into Tests:Database:*) lives in that NuGet package's own assembly, so it never runs for this
    // one -- without this, TestDatabaseFactory throws "Unsupported test database provider".
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
    public async Task ResolveAsync_DirectMatch_AgainstRealContentType()
    {
        var pageType = ContentTypeBuilder.CreateSimpleContentType("page", "Page");
        var createResult = await ContentTypeService.CreateAsync(pageType, Constants.Security.SuperUserKey);
        Assert.That(createResult.Success, Is.True);

        await PolicyService.SavePolicyAsync(new ContentCheckPolicy
        {
            Alias = "direct-policy",
            Name = "Direct policy",
            ContentTypeKey = pageType.Key,
            Rules = [new ContentCheckPolicyRule { CheckAlias = "required-property" }],
        });

        var content = ContentBuilder.CreateSimpleContent(pageType, "Home");
        ContentService.Save(content);

        var targets = await ResolutionService.ResolveAsync(content);

        Assert.That(targets, Has.Count.EqualTo(1));
        Assert.That(targets[0].Snapshot.Key, Is.EqualTo(content.Key));
        Assert.That(targets[0].Snapshot.VersionId, Is.EqualTo(content.VersionId));
        Assert.That(targets[0].Snapshot.HasProperty("title"), Is.True);
        Assert.That(targets[0].PolicyMatches.Single().Provenance, Is.EqualTo(ContentCheckEffectiveCheckProvenance.Direct));
    }

    [Test]
    public async Task ResolveAsync_CompositionMatch_AgainstRealComposedContentType()
    {
        var seoComposition = ContentTypeBuilder.CreateMetaContentType("seo", "SEO");
        var seoCreateResult = await ContentTypeService.CreateAsync(seoComposition, Constants.Security.SuperUserKey);
        Assert.That(seoCreateResult.Success, Is.True);

        var blogPostType = ContentTypeBuilder.CreateSimpleContentType("blogPost", "Blog Post");
        var blogPostCreateResult = await ContentTypeService.CreateAsync(blogPostType, Constants.Security.SuperUserKey);
        Assert.That(blogPostCreateResult.Success, Is.True);

        // A real composition (mixin), not parent/child inheritance -- exercises the real
        // IContentType.CompositionKeys() the resolution service reads, not the mock's stand-in.
        blogPostType.AddContentType(seoComposition);
        var composeResult = await ContentTypeService.CreateAsync(blogPostType, Constants.Security.SuperUserKey);
        Assert.That(composeResult.Success, Is.True);

        await PolicyService.SavePolicyAsync(new ContentCheckPolicy
        {
            Alias = "seo-baseline",
            Name = "SEO baseline",
            ContentTypeKey = seoComposition.Key,
            Rules = [new ContentCheckPolicyRule { CheckAlias = "required-property" }],
        });

        var content = ContentBuilder.CreateSimpleContent(blogPostType, "My Post");
        ContentService.Save(content);

        var targets = await ResolutionService.ResolveAsync(content);

        Assert.That(targets, Has.Count.EqualTo(1));
        Assert.That(targets[0].PolicyMatches.Single().Provenance, Is.EqualTo(ContentCheckEffectiveCheckProvenance.Composition));
        Assert.That(targets[0].PolicyMatches.Single().Policy.ContentTypeKey, Is.EqualTo(seoComposition.Key));
    }
}
