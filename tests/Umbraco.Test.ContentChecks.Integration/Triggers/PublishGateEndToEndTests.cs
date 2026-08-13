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

    /// <summary>
    /// Diagnostic for a real bug report: an editor sees a genuinely failing check (default
    /// FailOpen, real Fail status), but "Save and publish" still goes through. Reproduces the
    /// backoffice's actual sequence -- a Save call immediately before Publish, even with nothing
    /// left to change -- to check whether that alone invalidates the just-run report's
    /// ContentStamp (which keys off UpdateDate) before the gate ever sees it.
    /// </summary>
    [Test]
    public async Task DIAGNOSTIC_NoOpSaveImmediatelyBeforePublish_DoesItInvalidateTheStamp()
    {
        var pageType = ContentTypeBuilder.CreateSimpleContentType("page", "Page");
        await ContentTypeService.CreateAsync(pageType, Constants.Security.SuperUserKey);

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
                    // Default -- most rules will be left at this. If a no-op save invalidates the
                    // stamp, a FailOpen rule's real Fail becomes invisible to the gate entirely.
                    NotEvaluatedPolicy = ContentCheckNotEvaluatedPolicy.FailOpen,
                },
            ],
        });

        await GuardService.SaveAsync(new Guard
        {
            Name = "Site-wide",
            Rules = [new GuardRule { BlockAt = RequirementLevel.Mandatory }],
        });

        // Title deliberately left empty -- the check will genuinely Fail.
        var content = ContentBuilder.CreateSimpleContent(pageType, "Home");
        content.SetValue(PropertyAlias, string.Empty);
        ContentService.Save(content);

        var beforeRunChecks = ContentService.GetById(content.Key)!;
        TestContext.Out.WriteLine($"UpdateDate before RunChecksAsync : {beforeRunChecks.UpdateDate:O}");

        await RunChecksAsync(beforeRunChecks);

        var afterRunChecks = ContentService.GetById(content.Key)!;
        TestContext.Out.WriteLine($"UpdateDate after RunChecksAsync  : {afterRunChecks.UpdateDate:O}");

        // The backoffice's "Save and publish" issues a Save call right before Publish, even when
        // the editor made no further edits since the last save.
        ContentService.Save(afterRunChecks);

        var afterNoOpSave = ContentService.GetById(content.Key)!;
        TestContext.Out.WriteLine($"UpdateDate after no-op Save      : {afterNoOpSave:O}");
        TestContext.Out.WriteLine($"Stamp moved by the no-op save?   : {afterRunChecks.UpdateDate != afterNoOpSave.UpdateDate}");

        var publishResult = await PublishAsync(content.Key);
        TestContext.Out.WriteLine($"Publish succeeded (should be False -- the check genuinely fails) : {publishResult.Success}");

        Assert.That(
            publishResult.Success,
            Is.False,
            "A genuinely failing Mandatory check must still block, even after a no-op save right before publish.");
    }

    /// <summary>
    /// Second diagnostic: what if the content was edited (a real value change, not a no-op save)
    /// after the last check run, and the rule is left at the default FailOpen? A real, still-true
    /// Fail becomes invisible once the report goes stale, and FailOpen -- the default -- doesn't
    /// require a fresh report to let a publish through. If this is what the editor hit, it's
    /// working exactly as designed, just surprising if they don't know FailOpen is the default.
    /// </summary>
    [Test]
    public async Task DIAGNOSTIC_EditAfterRunningChecks_WithDefaultFailOpen_LetsAStillFailingCheckThrough()
    {
        var pageType = ContentTypeBuilder.CreateSimpleContentType("page", "Page");
        await ContentTypeService.CreateAsync(pageType, Constants.Security.SuperUserKey);

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
                    NotEvaluatedPolicy = ContentCheckNotEvaluatedPolicy.FailOpen, // the default
                },
            ],
        });

        await GuardService.SaveAsync(new Guard
        {
            Name = "Site-wide",
            Rules = [new GuardRule { BlockAt = RequirementLevel.Mandatory }],
        });

        var content = ContentBuilder.CreateSimpleContent(pageType, "Home");
        content.SetValue(PropertyAlias, string.Empty); // check will Fail
        ContentService.Save(content);

        await RunChecksAsync(content);

        // A real edit -- to an unrelated property, the failing one is still empty -- after the
        // report was generated. The stored Fail result is now stale.
        var reloaded = ContentService.GetById(content.Key)!;
        reloaded.Name = "Home (edited)";
        ContentService.Save(reloaded);

        var publishResult = await PublishAsync(reloaded.Key);
        TestContext.Out.WriteLine($"Publish succeeded (the check is still genuinely failing) : {publishResult.Success}");

        Assert.That(
            publishResult.Success,
            Is.True,
            "DIAGNOSTIC: with the default FailOpen, a stale report doesn't block, even though the underlying check would still fail if re-run.");
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
            RuleKey = entry.Entry.RuleKey,
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
