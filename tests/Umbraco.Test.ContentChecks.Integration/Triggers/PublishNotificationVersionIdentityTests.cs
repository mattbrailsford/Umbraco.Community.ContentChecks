using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.ContentPublishing;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Tests.Common.Builders;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;
using Umbraco.Community.ContentChecks.Extensions;

namespace Umbraco.Community.ContentChecks.Tests.Integration.Triggers;

/// <summary>
/// Answers the one question the Triggers design rests on: at the moment
/// <c>ContentPublishingNotification</c> fires, does the content carry a version id a previously
/// completed check run could have been recorded against, or a brand-new one no run can ever match?
/// Everything about reading a stored result at the publish gate (rather than evaluating inline)
/// depends on the answer, so it's measured here rather than inferred.
/// </summary>
[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public class PublishNotificationVersionIdentityTests : UmbracoIntegrationTest
{
    private IContentTypeService ContentTypeService => GetRequiredService<IContentTypeService>();

    private IContentService ContentService => GetRequiredService<IContentService>();

    private IContentPublishingService ContentPublishingService => GetRequiredService<IContentPublishingService>();

    protected override void CustomTestSetup(IUmbracoBuilder builder)
    {
        builder.AddContentChecks();
        builder.AddNotificationAsyncHandler<ContentSavingNotification, VersionCapturingHandler>();
        builder.AddNotificationAsyncHandler<ContentSavedNotification, VersionCapturingHandler>();
        builder.AddNotificationAsyncHandler<ContentPublishingNotification, VersionCapturingHandler>();
        builder.AddNotificationAsyncHandler<ContentPublishedNotification, VersionCapturingHandler>();
    }

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

    [SetUp]
    public void ClearCaptures() => VersionCapturingHandler.Captures.Clear();

    [Test]
    public async Task VersionId_AcrossSaveAndPublish()
    {
        var pageType = ContentTypeBuilder.CreateSimpleContentType("page", "Page");
        var createResult = await ContentTypeService.CreateAsync(pageType, Constants.Security.SuperUserKey);
        Assert.That(createResult.Success, Is.True);

        // 1. First save.
        var content = ContentBuilder.CreateSimpleContent(pageType, "Home");
        ContentService.Save(content);
        var afterFirstSave = content.VersionId;

        // 2. Second save, nothing changed.
        ContentService.Save(content);
        var afterNoOpSave = content.VersionId;

        // 3. Third save, a property actually changed.
        content.SetValue("title", "Changed title");
        ContentService.Save(content);
        var afterDirtySave = content.VersionId;

        VersionCapturingHandler.Captures.Clear();

        // 4. Publish an already-saved, non-dirty item -- the "editor saved, ran checks, then hit
        //    Publish" flow the stored-result gate design depends on.
        var publishResult = await ContentPublishingService.PublishAsync(
            content.Key,
            [new CulturePublishScheduleModel { Culture = null, Schedule = null }],
            Constants.Security.SuperUserKey);

        var reloaded = ContentService.GetById(content.Key)!;

        TestContext.Out.WriteLine($"publish succeeded          : {publishResult.Success}");
        TestContext.Out.WriteLine($"version after 1st save     : {afterFirstSave}");
        TestContext.Out.WriteLine($"version after no-op save   : {afterNoOpSave}");
        TestContext.Out.WriteLine($"version after dirty save   : {afterDirtySave}");
        TestContext.Out.WriteLine($"version after publish      : {reloaded.VersionId}");
        TestContext.Out.WriteLine($"publishedVersionId         : {reloaded.PublishedVersionId}");
        TestContext.Out.WriteLine("--- notifications, in order ---");
        foreach (var capture in VersionCapturingHandler.Captures)
        {
            TestContext.Out.WriteLine(capture);
        }

        var publishing = VersionCapturingHandler.Captures
            .FirstOrDefault(c => c.StartsWith("ContentPublishingNotification", StringComparison.Ordinal));

        Assert.That(publishing, Is.Not.Null, "ContentPublishingNotification never fired.");

        // The assertion that matters: whatever the gate sees must be the version the editor's last
        // completed run was recorded against (afterDirtySave), not something new.
        Assert.That(
            publishing,
            Does.Contain($"versionId={afterDirtySave}"),
            "The publish gate sees a different version than the last saved one -- a stored run can never match it.");
    }

    /// <summary>
    /// The version-keyed result cache (Core Contract §5) assumes a content item's version id
    /// changes when its values change. If a draft save mutates values in place under the same
    /// version id, a cached result is stale but indistinguishable from a fresh one. Reloads from
    /// the service each time rather than trusting the in-memory instance's own <c>VersionId</c>.
    /// </summary>
    [Test]
    public async Task VersionId_DoesItChangeOnDraftSave()
    {
        var pageType = ContentTypeBuilder.CreateSimpleContentType("page", "Page");
        await ContentTypeService.CreateAsync(pageType, Constants.Security.SuperUserKey);

        var content = ContentBuilder.CreateSimpleContent(pageType, "Home");
        ContentService.Save(content);

        var afterCreate = ContentService.GetById(content.Key)!;
        TestContext.Out.WriteLine($"after create : versionId={afterCreate.VersionId} title='{afterCreate.GetValue<string>("title")}' updated={afterCreate.UpdateDate:O}");

        content.SetValue("title", "First edit");
        ContentService.Save(content);
        var afterEditOne = ContentService.GetById(content.Key)!;
        TestContext.Out.WriteLine($"after edit 1 : versionId={afterEditOne.VersionId} title='{afterEditOne.GetValue<string>("title")}' updated={afterEditOne.UpdateDate:O}");

        content.SetValue("title", "Second edit");
        ContentService.Save(content);
        var afterEditTwo = ContentService.GetById(content.Key)!;
        TestContext.Out.WriteLine($"after edit 2 : versionId={afterEditTwo.VersionId} title='{afterEditTwo.GetValue<string>("title")}' updated={afterEditTwo.UpdateDate:O}");

        var allVersions = ContentService.GetVersions(content.Id).Select(v => v.VersionId).ToArray();
        TestContext.Out.WriteLine($"all version ids in db: {string.Join(", ", allVersions)}");

        // Characterization, not a wish: Umbraco updates a draft in place. Two saves with different
        // values leave one version row and one version id. Version id alone therefore cannot key a
        // result cache or a "has this been checked?" lookup -- a stale result is indistinguishable
        // from a current one. UpdateDate does move, and is the available discriminator.
        Assert.Multiple(() =>
        {
            Assert.That(afterEditTwo.VersionId, Is.EqualTo(afterCreate.VersionId), "draft save created a new version id");
            Assert.That(allVersions, Has.Length.EqualTo(1), "draft save created a new version row");
            Assert.That(afterEditTwo.GetValue<string>("title"), Is.EqualTo("Second edit"));
            Assert.That(afterEditTwo.UpdateDate, Is.GreaterThan(afterCreate.UpdateDate), "UpdateDate did not move on save");
        });
    }

    private sealed class VersionCapturingHandler :
        INotificationAsyncHandler<ContentSavingNotification>,
        INotificationAsyncHandler<ContentSavedNotification>,
        INotificationAsyncHandler<ContentPublishingNotification>,
        INotificationAsyncHandler<ContentPublishedNotification>
    {
        public static readonly List<string> Captures = [];

        public Task HandleAsync(ContentSavingNotification notification, CancellationToken cancellationToken)
            => Capture(nameof(ContentSavingNotification), notification.SavedEntities);

        public Task HandleAsync(ContentSavedNotification notification, CancellationToken cancellationToken)
            => Capture(nameof(ContentSavedNotification), notification.SavedEntities);

        public Task HandleAsync(ContentPublishingNotification notification, CancellationToken cancellationToken)
            => Capture(nameof(ContentPublishingNotification), notification.PublishedEntities);

        public Task HandleAsync(ContentPublishedNotification notification, CancellationToken cancellationToken)
            => Capture(nameof(ContentPublishedNotification), notification.PublishedEntities);

        private static Task Capture(string name, IEnumerable<IContent> entities)
        {
            foreach (var entity in entities)
            {
                Captures.Add(
                    $"{name}: versionId={entity.VersionId} publishedVersionId={entity.PublishedVersionId} " +
                    $"published={entity.Published} hasIdentity={entity.HasIdentity}");
            }

            return Task.CompletedTask;
        }
    }
}
