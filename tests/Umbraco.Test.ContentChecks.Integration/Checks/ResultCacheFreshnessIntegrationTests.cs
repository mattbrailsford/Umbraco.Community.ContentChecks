using System.Text.Json;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Tests.Common.Builders;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;
using Umbraco.Community.ContentChecks.Core.Checks;
using Umbraco.Community.ContentChecks.Core.Policies;
using Umbraco.Community.ContentChecks.Extensions;

namespace Umbraco.Community.ContentChecks.Tests.Integration.Checks;

/// <summary>
/// The regression guard for the Phase 1 cache defect (see Core Contract §5's defect box). The unit
/// suite could not catch it: <c>InMemoryContentCheckSnapshot</c> had no <c>VersionId</c>, so it
/// never took the cache path the bug lived on. It only reproduces against a real, saved
/// <c>IContent</c> — which is the whole lesson.
/// </summary>
[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public class ResultCacheFreshnessIntegrationTests : UmbracoIntegrationTest
{
    private IContentTypeService ContentTypeService => GetRequiredService<IContentTypeService>();

    private IContentService ContentService => GetRequiredService<IContentService>();

    private IContentCheckPolicyService PolicyService => GetRequiredService<IContentCheckPolicyService>();

    private IContentCheckEvaluationService EvaluationService => GetRequiredService<IContentCheckEvaluationService>();

    protected override void CustomTestSetup(IUmbracoBuilder builder) => builder.AddContentChecks();

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

    /// <summary>
    /// The exact workflow the defect broke: run checks, see a failure, fix the property, save, run
    /// again. Against the version-keyed cache this returned the stale <c>Fail</c>, because a draft
    /// save doesn't move the version id.
    /// </summary>
    [Test]
    public async Task Evaluate_AfterFixingTheFailingProperty_ReflectsTheNewValue()
    {
        var pageType = ContentTypeBuilder.CreateSimpleContentType("page", "Page");
        var createResult = await ContentTypeService.CreateAsync(pageType, Constants.Security.SuperUserKey);
        Assert.That(createResult.Success, Is.True);

        await PolicyService.SavePolicyAsync(new ContentCheckPolicy
        {
            Alias = "requires-title",
            Name = "Requires title",
            ContentTypeKey = pageType.Key,
            Rules =
            [
                new ContentCheckPolicyRule
                {
                    CheckAlias = "required-property",
                    Config = new ContentCheckConfig
                    {
                        Config = JsonSerializer.SerializeToElement(new { PropertyAlias = "title" }),
                    },
                },
            ],
        });

        var content = ContentBuilder.CreateSimpleContent(pageType, "Home");
        content.SetValue("title", string.Empty);
        ContentService.Save(content);

        var before = await EvaluationService.EvaluateAsync(content);
        Assert.That(
            before.Entries.Single().Entry.Result.Status,
            Is.EqualTo(ContentCheckStatus.Fail),
            "Setup failed: the empty required property should fail on the first run.");

        content.SetValue("title", "A real title");
        ContentService.Save(content);

        var reloaded = ContentService.GetById(content.Key)!;
        var after = await EvaluationService.EvaluateAsync(reloaded);

        Assert.That(
            after.Entries.Single().Entry.Result.Status,
            Is.EqualTo(ContentCheckStatus.Pass),
            "The cache returned the pre-edit verdict for post-edit content -- the defect is back.");
    }

    /// <summary>
    /// The other half: the cache still has to actually cache. A test that only asserts freshness
    /// would pass just as well against a cache that never hits at all.
    /// </summary>
    [Test]
    public async Task Evaluate_TwiceWithNoEdit_ReusesTheCachedResult()
    {
        var pageType = ContentTypeBuilder.CreateSimpleContentType("page", "Page");
        await ContentTypeService.CreateAsync(pageType, Constants.Security.SuperUserKey);

        await PolicyService.SavePolicyAsync(new ContentCheckPolicy
        {
            Alias = "requires-title",
            Name = "Requires title",
            ContentTypeKey = pageType.Key,
            Rules =
            [
                new ContentCheckPolicyRule
                {
                    CheckAlias = "required-property",
                    Config = new ContentCheckConfig
                    {
                        Config = JsonSerializer.SerializeToElement(new { PropertyAlias = "title" }),
                    },
                },
            ],
        });

        var content = ContentBuilder.CreateSimpleContent(pageType, "Home");
        ContentService.Save(content);

        var first = await EvaluationService.EvaluateAsync(content);
        var second = await EvaluationService.EvaluateAsync(content);

        // A cache hit returns the stored result object rather than re-running the check, so the
        // second run's elapsed time is the give-away -- there's no evaluation to spend it on.
        Assert.That(first.Entries.Single().Entry.Result.Status, Is.EqualTo(second.Entries.Single().Entry.Result.Status));
        Assert.That(
            second.Entries.Single().Entry.Elapsed,
            Is.LessThan(first.Entries.Single().Entry.Elapsed),
            "The second run doesn't look cached.");
    }
}
