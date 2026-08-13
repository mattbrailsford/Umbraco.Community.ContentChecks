using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.ContentPublishing;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.OperationStatus;
using Umbraco.Cms.Tests.Common.Builders;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;
using Umbraco.Community.ContentChecks.Core.Checks;
using Umbraco.Community.ContentChecks.Core.Policies;
using Umbraco.Community.ContentChecks.Extensions;
using Umbraco.Community.ContentChecks.Web.Triggers;

namespace Umbraco.Community.ContentChecks.Tests.Integration.Triggers;

/// <summary>
/// End-to-end proof of the scenario docs/guard-spec.md's publish-gate correction exists for:
/// a Guard that's meant to require a passing report can't be dodged just by publishing content
/// that's never been checked, or was edited since it last was. Runs against a real
/// <see cref="IContentPublishingService"/>, a real <see cref="ContentPublishingGateHandler"/>, and
/// real EF-backed content — not mocks standing in for any of them.
/// </summary>
[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public class PublishGateEndToEndTests : UmbracoIntegrationTest
{
    private const string PropertyAlias = "title";

    private IContentTypeService ContentTypeService => GetRequiredService<IContentTypeService>();

    private IContentService ContentService => GetRequiredService<IContentService>();

    private IContentPublishingService ContentPublishingService => GetRequiredService<IContentPublishingService>();

    private IContentCheckPolicyService PolicyService => GetRequiredService<IContentCheckPolicyService>();

    private IGuardService GuardService => GetRequiredService<IGuardService>();

    private IContentCheckEvaluationService EvaluationService => GetRequiredService<IContentCheckEvaluationService>();

    private IContentCheckResultService ResultService => GetRequiredService<IContentCheckResultService>();

    protected override void CustomTestSetup(IUmbracoBuilder builder)
    {
        builder.AddContentChecks();

        // The gate handler bails out entirely when there's no live HttpContext (a scheduled/
        // background publish never blocks) -- fake one so this test exercises the interactive path.
        builder.Services.AddSingleton<IHttpContextAccessor>(new FakeHttpContextAccessor());
        builder.Services.AddSingleton<IBackOfficeSecurityAccessor>(new FakeBackOfficeSecurityAccessor());
        builder.AddNotificationAsyncHandler<ContentPublishingNotification, ContentPublishingGateHandler>();
    }

    // Umbraco.Cms.Tests.Integration's own GlobalSetupTeardown lives in that NuGet package's own
    // assembly, so it never runs for this one -- without this, TestDatabaseFactory throws
    // "Unsupported test database provider".
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
    public async Task PublishGate_RequiresAFreshPassingReport_WhenTheRuleIsFailClosed()
    {
        var pageType = ContentTypeBuilder.CreateSimpleContentType("page", "Page");
        var createTypeResult = await ContentTypeService.CreateAsync(pageType, Constants.Security.SuperUserKey);
        Assert.That(createTypeResult.Success, Is.True);

        await PolicyService.SavePolicyAsync(new ContentCheckPolicy
        {
            Alias = "required-title",
            Name = "Required title",
            ContentTypeKey = pageType.Key,
            Rules =
            [
                new ContentCheckPolicyRule
                {
                    CheckAlias = "required-property",
                    Config = new ContentCheckConfig
                    {
                        Config = JsonSerializer.SerializeToElement(new { propertyAlias = PropertyAlias }),
                    },
                    RequirementLevel = RequirementLevel.Mandatory,
                    NotEvaluatedPolicy = ContentCheckNotEvaluatedPolicy.FailClosed,
                },
            ],
        });

        await GuardService.SaveAsync(new Guard
        {
            Name = "Site-wide",
            Rules = [new GuardRule { BlockAt = RequirementLevel.Mandatory }],
        });

        var content = ContentBuilder.CreateSimpleContent(pageType, "Home");
        content.SetValue(PropertyAlias, "A real title");
        ContentService.Save(content);

        // 1. Never checked yet -- a FailClosed Mandatory rule with no report at all must block.
        var firstAttempt = await PublishAsync(content.Key);
        Assert.That(firstAttempt.Success, Is.False, "Publishing with no check report at all should be blocked by the FailClosed rule.");

        // 2. Run checks for real (the same evaluation/persist steps the background run queue
        //    performs) against the content exactly as it stands right now.
        await RunChecksAsync(content);

        // 3. Same content, now with a fresh, passing report -- must succeed.
        var secondAttempt = await PublishAsync(content.Key);
        Assert.That(secondAttempt.Success, Is.True, "Publishing with a fresh, passing report should succeed.");

        // 4. Edit the content again without re-running checks -- the stored report is now stale
        //    (it predates this edit), so it must be treated like no report at all and block again.
        var reloaded = ContentService.GetById(content.Key)!;
        reloaded.SetValue(PropertyAlias, "A different real title");
        ContentService.Save(reloaded);

        var thirdAttempt = await PublishAsync(reloaded.Key);
        Assert.That(thirdAttempt.Success, Is.False, "A stale report (content edited since it ran) must not carry over to a new publish attempt.");
    }

    private async Task<Attempt<ContentPublishingResult, ContentPublishingOperationStatus>> PublishAsync(Guid contentKey)
        => await ContentPublishingService.PublishAsync(
            contentKey,
            [new CulturePublishScheduleModel { Culture = null, Schedule = null }],
            Constants.Security.SuperUserKey);

    /// <summary>
    /// Mirrors <c>ContentCheckRunBackgroundService.ProcessItemAsync</c>'s evaluate-then-persist
    /// steps directly, rather than driving the queue/tracker/background-service plumbing that
    /// exists purely to stream progress to a watching editor -- irrelevant to what this test
    /// is proving.
    /// </summary>
    private async Task RunChecksAsync(IContent content)
    {
        var report = await EvaluationService.EvaluateAsync(content);
        var ranAtUtc = DateTime.UtcNow;
        var storedResults = report.Entries.Select(entry => new ContentCheckStoredResult
        {
            ContentKey = content.Key,
            ContentStamp = report.ContentStamp,
            Path = entry.Path,
            CheckAlias = entry.Entry.CheckAlias,
            RequirementLevel = entry.Entry.RequirementLevel,
            NotEvaluatedPolicy = entry.Entry.NotEvaluatedPolicy,
            Result = entry.Entry.Result,
            RanAtUtc = ranAtUtc,
        }).ToList();

        await ResultService.ReplaceForContentAsync(content.Key, culture: null, segment: null, storedResults);
    }

    private sealed class FakeHttpContextAccessor : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; } = new DefaultHttpContext();
    }

    private sealed class FakeBackOfficeSecurityAccessor : IBackOfficeSecurityAccessor
    {
        public IBackOfficeSecurity? BackOfficeSecurity => null;
    }
}
