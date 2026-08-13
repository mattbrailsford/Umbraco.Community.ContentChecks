using Moq;
using NUnit.Framework;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Community.ContentChecks.Core.Checks;
using Umbraco.Community.ContentChecks.Core.Policies;
using Umbraco.Community.ContentChecks.Core.Policies.Elements;
using Umbraco.Community.ContentChecks.Tests.Unit.Checks;

namespace Umbraco.Community.ContentChecks.Tests.Unit.Policies;

[TestFixture]
public class ContentCheckResolutionServiceTests
{
    private static FakeContentCheck PassingCheck(string alias, ContentCheckCost cost = ContentCheckCost.Free, ContentCheckKind kind = ContentCheckKind.Deterministic)
        => new(alias, cost, kind, _ => new ContentCheckResult { Status = ContentCheckStatus.Pass, Message = "ok" });

    private static ContentCheckResolutionService CreateService(
        FakeContentCheckPolicyService policyService,
        IEnumerable<IContentCheck> checks,
        IEnumerable<IContentCheckElementSource> elementSources,
        Mock<IContentTypeService> contentTypeService)
        => new(
            policyService,
            new ContentCheckCollection(() => checks),
            new ContentCheckElementSourceCollection(() => elementSources),
            contentTypeService.Object,
            new Mock<IEntityService>().Object);

    [Test]
    public async Task ResolveAsync_DirectMatch_ReturnsRootTargetWithItsRules()
    {
        var contentTypeKey = Guid.NewGuid();
        var contentType = ContentTestFactory.ContentType(contentTypeKey);
        var contentTypeService = new Mock<IContentTypeService>();
        contentTypeService.Setup(s => s.GetAsync(contentTypeKey)).ReturnsAsync(contentType.Object);

        var check = PassingCheck("required-property");
        var policyService = new FakeContentCheckPolicyService();
        policyService.AddPolicy(new ContentCheckPolicy
        {
            Alias = "direct-policy",
            Name = "Direct policy",
            ContentTypeKey = contentTypeKey,
            Rules = [new ContentCheckPolicyRule { CheckAlias = check.Alias }],
        });

        var service = CreateService(policyService, [check], [], contentTypeService);
        var content = ContentTestFactory.Content(contentTypeKey);

        var targets = await service.ResolveAsync(content.Object);

        Assert.That(targets, Has.Count.EqualTo(1));
        Assert.That(targets[0].PolicyMatches.SelectMany(m => m.Rules).Select(r => r.Check.Alias), Is.EqualTo(new[] { "required-property" }));
        Assert.That(targets[0].PolicyMatches.Single().Provenance, Is.EqualTo(ContentCheckEffectiveCheckProvenance.Direct));
        Assert.That(targets[0].Path, Is.Empty);
    }

    [Test]
    public async Task ResolveAsync_CompositionMatch_TransitiveTwoHop_Matches()
    {
        // BlogPost composes (transitively) from Seo. CompositionKeys() already flattens this in
        // real Umbraco -- the mock just returns what that flattening would produce.
        var seoKey = Guid.NewGuid();
        var blogPostKey = Guid.NewGuid();
        var blogPostType = ContentTestFactory.ContentType(blogPostKey, compositionKeys: [seoKey]);
        var contentTypeService = new Mock<IContentTypeService>();
        contentTypeService.Setup(s => s.GetAsync(blogPostKey)).ReturnsAsync(blogPostType.Object);

        var check = PassingCheck("seo-required");
        var policyService = new FakeContentCheckPolicyService();
        policyService.AddPolicy(new ContentCheckPolicy
        {
            Alias = "seo-baseline",
            Name = "SEO baseline",
            ContentTypeKey = seoKey,
            Rules = [new ContentCheckPolicyRule { CheckAlias = check.Alias }],
        });

        var service = CreateService(policyService, [check], [], contentTypeService);
        var content = ContentTestFactory.Content(blogPostKey);

        var targets = await service.ResolveAsync(content.Object);

        Assert.That(targets, Has.Count.EqualTo(1));
        Assert.That(targets[0].PolicyMatches.SelectMany(m => m.Rules).Select(r => r.Check.Alias), Is.EqualTo(new[] { "seo-required" }));
        var match = targets[0].PolicyMatches.Single();
        Assert.That(match.Provenance, Is.EqualTo(ContentCheckEffectiveCheckProvenance.Composition));
        Assert.That(match.Policy.ContentTypeKey, Is.EqualTo(seoKey));
    }

    [Test]
    public async Task ResolveAsync_DirectAndCompositionPolicies_CombineByUnion_WithNoRelationshipBetweenRecords()
    {
        // A composition-wide baseline (attached to Seo) and a type-specific refinement (attached
        // directly to BlogPost) apply to the same content simultaneously, with no reference
        // between the two policy records.
        var seoKey = Guid.NewGuid();
        var blogPostKey = Guid.NewGuid();
        var blogPostType = ContentTestFactory.ContentType(blogPostKey, compositionKeys: [seoKey]);
        var contentTypeService = new Mock<IContentTypeService>();
        contentTypeService.Setup(s => s.GetAsync(blogPostKey)).ReturnsAsync(blogPostType.Object);

        var seoCheck = PassingCheck("seo-required");
        var titleCheck = PassingCheck("title-length");
        var policyService = new FakeContentCheckPolicyService();
        policyService.AddPolicy(new ContentCheckPolicy
        {
            Alias = "seo-baseline",
            Name = "SEO baseline",
            ContentTypeKey = seoKey,
            Rules = [new ContentCheckPolicyRule { CheckAlias = seoCheck.Alias }],
        });
        policyService.AddPolicy(new ContentCheckPolicy
        {
            Alias = "blog-post-refinement",
            Name = "Blog Post refinement",
            ContentTypeKey = blogPostKey,
            Rules = [new ContentCheckPolicyRule { CheckAlias = titleCheck.Alias }],
        });

        var service = CreateService(policyService, [seoCheck, titleCheck], [], contentTypeService);
        var content = ContentTestFactory.Content(blogPostKey);

        var targets = await service.ResolveAsync(content.Object);

        Assert.That(targets, Has.Count.EqualTo(1));
        Assert.That(
            targets[0].PolicyMatches.SelectMany(m => m.Rules).Select(r => r.Check.Alias),
            Is.EquivalentTo(new[] { "seo-required", "title-length" }));
    }

    [Test]
    public async Task ResolveAsync_CascadingMatch_ResolvesNestedElementsAtAnyDepth()
    {
        var pageKey = Guid.NewGuid();
        var testimonialKey = Guid.NewGuid();
        var featureCardKey = Guid.NewGuid();

        var pageType = ContentTestFactory.ContentType(
            pageKey,
            propertyTypes: [ContentTestFactory.PropertyType("content", "Umbraco.BlockList").Object]);
        var testimonialType = ContentTestFactory.ContentType(testimonialKey);
        var featureCardType = ContentTestFactory.ContentType(featureCardKey);

        var contentTypeService = new Mock<IContentTypeService>();
        contentTypeService.Setup(s => s.GetAsync(pageKey)).ReturnsAsync(pageType.Object);
        contentTypeService.Setup(s => s.GetAsync(testimonialKey)).ReturnsAsync(testimonialType.Object);
        contentTypeService.Setup(s => s.GetAsync(featureCardKey)).ReturnsAsync(featureCardType.Object);

        var testimonialInstance = new ContentCheckElementInstance
        {
            Key = Guid.NewGuid(),
            ContentTypeKey = testimonialKey,
            ContentTypeAlias = "testimonial",
            Values =
            [
                new ContentCheckElementPropertyValue
                {
                    Alias = "nested",
                    PropertyEditorAlias = "Umbraco.BlockList",
                    Value = "nested-json",
                },
            ],
        };
        var featureCardInstance = new ContentCheckElementInstance
        {
            Key = Guid.NewGuid(),
            ContentTypeKey = featureCardKey,
            ContentTypeAlias = "featureCard",
            Values = [],
        };

        var elementSource = new FakeContentCheckElementSourceByValue("Umbraco.BlockList", new Dictionary<object, IReadOnlyList<ContentCheckElementInstance>>
        {
            ["root-json"] = [testimonialInstance],
            ["nested-json"] = [featureCardInstance],
        });

        var quoteCheck = PassingCheck("quote-required", ContentCheckCost.Metered, ContentCheckKind.Nondeterministic);
        var iconCheck = PassingCheck("icon-required");
        var policyService = new FakeContentCheckPolicyService();
        policyService.AddPolicy(new ContentCheckPolicy
        {
            Alias = "testimonial-policy",
            Name = "Testimonial",
            ContentTypeKey = testimonialKey,
            Rules = [new ContentCheckPolicyRule { CheckAlias = quoteCheck.Alias }],
        });
        policyService.AddPolicy(new ContentCheckPolicy
        {
            Alias = "feature-card-policy",
            Name = "Feature Card",
            ContentTypeKey = featureCardKey,
            Rules = [new ContentCheckPolicyRule { CheckAlias = iconCheck.Alias }],
        });

        var service = CreateService(policyService, [quoteCheck, iconCheck], [elementSource], contentTypeService);

        var content = ContentTestFactory.Content(pageKey, properties: [ContentTestFactory.Property(ContentTestFactory.PropertyType("content", "Umbraco.BlockList").Object)]);
        content.SetupGetValue("content", "root-json");

        var targets = await service.ResolveAsync(content.Object);

        Assert.That(targets, Has.Count.EqualTo(2));

        var testimonialTarget = targets.Single(t => t.PolicyMatches.SelectMany(m => m.Rules).Any(r => r.Check.Alias == "quote-required"));
        Assert.That(testimonialTarget.Path, Has.Count.EqualTo(1));
        Assert.That(testimonialTarget.Path[0].PropertyAlias, Is.EqualTo("content"));
        Assert.That(testimonialTarget.Path[0].ElementKey, Is.EqualTo(testimonialInstance.Key));
        Assert.That(testimonialTarget.Snapshot.ContentTypeAlias, Is.EqualTo("testimonial"));
        Assert.That(testimonialTarget.PolicyMatches.Single().Provenance, Is.EqualTo(ContentCheckEffectiveCheckProvenance.Cascading));

        var featureCardTarget = targets.Single(t => t.PolicyMatches.SelectMany(m => m.Rules).Any(r => r.Check.Alias == "icon-required"));
        Assert.That(featureCardTarget.Path, Has.Count.EqualTo(2));
        Assert.That(featureCardTarget.Path[0].PropertyAlias, Is.EqualTo("content"));
        Assert.That(featureCardTarget.Path[0].ElementKey, Is.EqualTo(testimonialInstance.Key));
        Assert.That(featureCardTarget.Path[1].PropertyAlias, Is.EqualTo("nested"));
        Assert.That(featureCardTarget.Path[1].ElementKey, Is.EqualTo(featureCardInstance.Key));
        Assert.That(featureCardTarget.Snapshot.ContentTypeAlias, Is.EqualTo("featureCard"));
    }

    [Test]
    public async Task ResolveAsync_CascadingCost_NInstancesOfAMeteredCheck_ProducesExactlyNTargets()
    {
        // Cascading multiplies Metered calls by instance count -- a deliberate, visible decision
        // (open question 7), not an accident of traversal order. Pinning the count down here
        // means a future change that silently batches/caps/dedupes this will fail a test, not
        // slip through unnoticed.
        var pageKey = Guid.NewGuid();
        var testimonialKey = Guid.NewGuid();

        var pageType = ContentTestFactory.ContentType(
            pageKey,
            propertyTypes: [ContentTestFactory.PropertyType("content", "Umbraco.BlockList").Object]);
        var testimonialType = ContentTestFactory.ContentType(testimonialKey);

        var contentTypeService = new Mock<IContentTypeService>();
        contentTypeService.Setup(s => s.GetAsync(pageKey)).ReturnsAsync(pageType.Object);
        contentTypeService.Setup(s => s.GetAsync(testimonialKey)).ReturnsAsync(testimonialType.Object);

        var instances = Enumerable.Range(0, 3)
            .Select(_ => new ContentCheckElementInstance
            {
                Key = Guid.NewGuid(),
                ContentTypeKey = testimonialKey,
                ContentTypeAlias = "testimonial",
                Values = [],
            })
            .ToList();

        var elementSource = new FakeContentCheckElementSource("Umbraco.BlockList", elementInstances: instances);

        var quoteCheck = PassingCheck("quote-required", ContentCheckCost.Metered, ContentCheckKind.Nondeterministic);
        var policyService = new FakeContentCheckPolicyService();
        policyService.AddPolicy(new ContentCheckPolicy
        {
            Alias = "testimonial-policy",
            Name = "Testimonial",
            ContentTypeKey = testimonialKey,
            Rules = [new ContentCheckPolicyRule { CheckAlias = quoteCheck.Alias, RequirementLevel = RequirementLevel.Optional }],
        });

        var service = CreateService(policyService, [quoteCheck], [elementSource], contentTypeService);
        var content = ContentTestFactory.Content(pageKey, properties: [ContentTestFactory.Property(ContentTestFactory.PropertyType("content", "Umbraco.BlockList").Object)]);
        content.SetupGetValue("content", "any-value");

        var targets = await service.ResolveAsync(content.Object);

        Assert.That(targets, Has.Count.EqualTo(3));
        Assert.That(targets, Has.All.Matches<ContentCheckResolvedTarget>(
            t => t.PolicyMatches.SelectMany(m => m.Rules).Any(r => r.Check.Alias == "quote-required")));
    }
}
