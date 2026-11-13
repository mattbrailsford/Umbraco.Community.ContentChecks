# Content Checks — Markup-Based Checking (Exploration)

**Status:** Exploratory. This captures the full extent of an idea, not a build-ready spec. Some things got settled along the way (marked **Settled**); most of the interesting parts are still genuinely open (marked **Open**) and are recorded as forks with their reasoning, not as decisions. Narrowing to what actually ships is a deliberate, separate, later step — nothing here should be read as "cut" or "deferred" just because it's unresolved. **Real groundwork is now built, ahead of the rest of this (2026-11-13):** the preview-credential seam (§9.4) and the default `IContentCheckMarkupProvider` itself (§9.2, §9.3) — request/response shape, the HTTP-fetch implementation, URL resolution, and the preview cookie all wired together, unit-tested. **The `IContentCheck`/`IPropertyContentCheck`/`IMarkupContentCheck` contract split (§4) is now design-settled, same day** — a pre-build pass that caught a real bug before any code existed (a markup rule would otherwise wrongly cascade into nested elements, §4.5) — but not yet implemented. URL resolution's real-site behavior also remains unverified (§9.3's "Not yet proven" note).

**Companion to, not part of:** [Overview](overview.md) and its specs ([Core Contract](core-contract-spec.md), [Policy Resolution](policy-resolution-spec.md), [Management API & UI](management-api-ui-spec.md), [Triggers](triggers-spec.md), [AI Integration](ai-integration-spec.md)). Nothing here has been merged into those. If and when parts of this narrow down to something buildable, they get folded in explicitly, not implicitly.

---

## 1. The idea

Checks that currently only target properties (`IContentCheckSnapshot`, per [Core Contract](core-contract-spec.md)) could also target the *rendered output markup* of a page — the actual HTML a browser or search engine receives, not the raw stored content.

## 2. Why this is a distinct need, not a variant of property checking

Several real check types are fundamentally about the rendered page, not any one property:

- **Accessibility** — "every `<img>` has `alt`." Alt text can be composed from more than one property, dropped by a template bug even when the property itself is fine, or belong to an image injected by a partial/global element (site logo, nav) that isn't part of the content being checked at all. Only the rendered page can see any of that.
- **SEO/technical structure** — exactly one `<h1>`, a `<title>` tag, a canonical link, valid JSON-LD structured data. These are about what a browser or search engine actually receives, which depends on the full render (layout, partials, templates), not one content item's own data.
- **Broken-link checking** on the actual page, including links a template injects that never lived in any property.
- **AI holistic/visual quality judgment** — an agent judging "does this page look right" from the actual rendered page is closer to how a human reviewer would judge it than reconstructing an impression from disassembled property values.

## 3. The headless complication

A decoupled Umbraco site has no server-side rendering happening on the CMS side at all — Umbraco is a data API; something else entirely (different repo, different framework, maybe different hosting) renders the page. There is no default "ask Umbraco for the markup" that works universally. This has to be a pluggable extension point from day one, not an assumed capability with headless bolted on as an afterthought.

## 4. Contract shape — **Settled** (pre-build design pass, 2026-11-13)

Checked directly against the real current source in `src/Umbraco.Community.ContentChecks.Core/Checks/` (`IContentCheck.cs`, `ContentCheckBase.cs`, `ContentCheckRunner.cs`) and `Policies/ContentCheckResolutionService.cs`, not assumed from this doc's own older sketch — the docs in this project have already gone stale twice this session (see §9.2/§9.3's history), so this pass re-verified against real code before settling anything. The user confirmed this contract has never shipped, so the brief was explicit: reshape `IContentCheck` however produces the best design, not whatever avoids a breaking change.

### 4.1 Final interface/base-class shape

`IContentCheckSnapshot` is a property bag; markup is a raw HTML string. These are different input shapes, not the same shape stretched to fit, so property checks and markup checks don't share one `EvaluateAsync` signature:

```csharp
// Metadata only. No EvaluateAsync -- a bare IContentCheck can't run anything.
public interface IContentCheck : IDiscoverable
{
    string Alias { get; }
    string Name { get; }
    string Description { get; }
    ContentCheckCost Cost { get; }
    ContentCheckKind Kind { get; }
    Type? ConfigType { get; }
    ContentCheckConfigSchema? GetConfigSchema();
}

public interface IPropertyContentCheck : IContentCheck
{
    Task<ContentCheckResult> EvaluateAsync(ContentCheckContext context, ContentCheckConfig config, CancellationToken cancellationToken);
}

public interface IMarkupContentCheck : IContentCheck
{
    Task<ContentCheckResult> EvaluateAsync(ContentCheckMarkupContext context, ContentCheckConfig config, CancellationToken cancellationToken);
}
```

This is a **mandatory branch, not an optional add-on** — a meaningfully different relationship than `IFixableContentCheck` (considered and rejected in [Core Contract](core-contract-spec.md)), which would have been additive on top of an always-runnable base. Here, `IContentCheck` alone describes something but can't run anything; you have to be one of the two children to be runnable. Anything that actually executes a check (not just lists it) has to pattern-match which one it's holding (§4.4).

Base classes mirror the split. Reading the real `ContentCheckBase<TConfig>` shows exactly what the shared, foundational piece is — it's already all in one place today, not vague:

```csharp
// Everything ContentCheckBase<TConfig> does today MINUS the EvaluateAsync proxy: reads
// [ContentCheck] off the derived type, Alias/Name/Cost/Kind/ConfigType, lazy schema,
// GetConfigSchema(), ResolveConfig(), CreateDefaultConfig(). Relocated unchanged.
public abstract class ContentCheckMetadataBase<TConfig> : IContentCheck { /* ... */ }

// What ContentCheckBase<TConfig> is renamed to -- see the naming decision below.
public abstract class PropertyContentCheckBase<TConfig>
    : ContentCheckMetadataBase<TConfig>, IPropertyContentCheck
{
    // Exactly today's EvaluateAsync(ContentCheckContext, ContentCheckConfig, ct) proxy
    // + the abstract typed overload, unchanged.
}

public abstract class MarkupContentCheckBase<TConfig>
    : ContentCheckMetadataBase<TConfig>, IMarkupContentCheck
{
    public Task<ContentCheckResult> EvaluateAsync(
        ContentCheckMarkupContext context, ContentCheckConfig config, CancellationToken cancellationToken)
    {
        if (!context.Markup.Success)
        {
            return Task.FromResult(new ContentCheckResult
            {
                Status = ContentCheckStatus.NotEvaluated,
                Message = context.Markup.FailureReason ?? "The page could not be fetched.",
                NotEvaluatedReason = ContentCheckNotEvaluatedReason.Error,
            });
        }

        return EvaluateAsync(context, ResolveConfig(config) ?? CreateDefaultConfig(), cancellationToken);
    }

    protected abstract Task<ContentCheckResult> EvaluateAsync(
        ContentCheckMarkupContext context, TConfig config, CancellationToken cancellationToken);
}
```

**The fetch-failure short-circuit lives in `MarkupContentCheckBase`, not in every check.** Mirrors the existing philosophy exactly: `ContentCheckBase<TConfig>`'s whole reason for existing is to stop every check repeating "resolve config, default if missing" ([Core Contract](core-contract-spec.md) §2's note on this exact refactor). A markup check having to repeat "did the fetch even succeed?" at the top of every `EvaluateAsync` is the same category of boilerplate. Centralizing it means a real markup check's body can assume valid HTML, the same way a property check assumes a resolved, defaulted config.

**Naming — rename `ContentCheckBase<TConfig>` to `PropertyContentCheckBase<TConfig>`.** Not required (nothing ships yet, so no forced migration either way), but recommended: with a markup branch existing, `ContentCheckBase` no longer means "the base for any check" — `ContentCheckMetadataBase` does. Keeping the old name on the property branch would read as if it's still the universal one. Touches 3 files, each a one-word change — see §4.6.

### 4.2 Who calls `IContentCheckMarkupProvider`, and when

**Decision: `ContentCheckRunner` calls `IContentCheckMarkupProvider.GetMarkupAsync` directly — individual markup checks never call it.** This is the answer to §11's long-flagged "fetch markup once per run, share across checks" optimization, and it falls out cleanly from how `ContentCheckRunner.RunAsync` is already structured:

- `RunAsync` already distinguishes `Cost.Free` (sequential, always runs) from `Cost.Metered` (concurrent, short-circuited if a mandatory Free rule failed). A markup fetch is exactly the same shape of cost as any other `Metered` check: expensive, worth skipping if short-circuited.
- Add one field to `RunAsync`: `Lazy<Task<ContentCheckMarkupResult>>? markupFetch`, created only if the request actually contains a markup-check rule, built from `IContentCheckMarkupProvider.GetMarkupAsync(new ContentCheckMarkupRequest { ContentKey = request.Content.Key, Culture = request.Culture, Segment = request.Segment }, cancellationToken)`.
- Thread it into the per-rule execution path. The fetch is only triggered (`markupFetch.Value` accessed) when the rule about to run is an `IMarkupContentCheck` — never unconditionally.
- **This preserves the existing short-circuit guarantee for free.** If a mandatory `Free` rule fails, metered rules are never run at all — so a `Metered`-cost markup check's lazy fetch is simply never triggered, exactly like any other skipped `Metered` check today. No special-casing needed; this is why the fetch must be triggered *inside* the per-rule path, not eagerly at the top of `RunAsync`.
- **`Lazy<Task<T>>`'s default thread-safety mode is the right primitive for the concurrent `Metered` path:** if two markup-check rules race to access `markupFetch.Value` concurrently, only one real fetch happens; both get the same in-flight `Task`.

**New constructor dependency: `ContentCheckRunner` needs `IContentCheckMarkupProvider` injected.** Already registered as a singleton (added alongside the provider itself) — this is a wiring addition, not a new registration. Every existing `ContentCheckRunnerTests` setup needs a `Mock<IContentCheckMarkupProvider>` added — mechanical, not risky, but real; see §4.6.

**Cross-run result caching already works correctly for a markup check with zero changes.** The runner's existing cache key (content stamp + content key + culture/segment + check alias + assembly version + config JSON) is available identically for a markup check, and a cache *hit* returns before ever reaching the markup-check branch — so an unchanged page's markup check doesn't even trigger a fetch on a re-run. The within-one-run `Lazy` sharing above is a *different, complementary* layer: it only matters when at least one markup rule misses the cross-run cache and more than one markup rule needs the same HTML in that run.

**Convention, not enforced:** a markup check should realistically always declare `Cost.Metered` (a network fetch is never actually free), but nothing forces this at the type level — consistent with this project's existing avoidance of compile-time constraints that don't earn their keep (see [Core Contract](core-contract-spec.md)'s note on why `ResolveConfig` avoids a `where TConfig : new()` constraint). Worth a doc comment on `IMarkupContentCheck`, not a runtime check.

### 4.3 `ContentCheckMarkupContext` shape

```csharp
public sealed class ContentCheckMarkupContext
{
    public required IContentCheckSnapshot Content { get; init; }
    public string? Culture { get; init; }
    public string? Segment { get; init; }
    public required ContentCheckMarkupResult Markup { get; init; }
}
```

Mirrors `ContentCheckContext` (`Content`/`Culture`/`Segment`) exactly, with `Markup` added to carry the already-fetched `ContentCheckMarkupResult` (§9.2's `Html`/`Url`/`StatusCode`/`Success`/`FailureReason`, unchanged). Giving a markup check `Content` too (not just the raw HTML) means it can report the content's identity the same way a property check can, without parsing it back out of the fetched `Url`.

**Dependency on §4.5 below:** this only works correctly because a markup-check rule will only ever be evaluated against the *root* `IContentCheckSnapshot`, never a nested element instance's — see §4.5's resolution-service fix, which is a prerequisite, not an independent nice-to-have. Without that fix, `Content.Key` here could be a nested block's key, which has no URL of its own and would make the markup request nonsensical.

### 4.4 `ContentCheckRunner` dispatch

```csharp
result = rule.Check switch
{
    IMarkupContentCheck markupCheck => await EvaluateMarkupRuleAsync(markupCheck, rule.Config, request, markupFetch!, cancellationToken),
    IPropertyContentCheck propertyCheck => await propertyCheck.EvaluateAsync(
        new ContentCheckContext { Content = request.Content, Culture = request.Culture, Segment = request.Segment },
        rule.Config, cancellationToken),
    _ => throw new InvalidOperationException($"Check '{rule.Check.Alias}' is neither a property nor a markup check."),
};
```

The `_ =>` branch is unreachable in practice (every real check goes through one of the two base classes), but earns its keep the same way `ContentCheckBase<TConfig>`'s own attribute-presence check does — a clear failure instead of a silent no-op if someone ever implements `IContentCheck` directly, which its own docs already say nobody does.

### 4.5 Policy Resolution — the one real bug this design pass caught

**Finding, not assumed:** `ContentCheckResolutionService`'s rule resolution resolves *every* rule in a policy by alias with **no filtering by check kind at all**, and the method building policy matches calls it identically whether building the root match (no cascading) or a nested element match (cascading). Today, if a policy contains both a property rule and a (not-yet-existing) markup rule, and that policy also matches a nested element type via cascading, the markup rule would be resolved as a *cascading* match at the nested location — directly contradicting §5's already-settled "whole-page-only, no cascading" decision. This isn't hypothetical once `IMarkupContentCheck` exists; it's the actual behavior of the unmodified resolver, caught by reading the real code before writing any of the new contract.

**Fix: the policy-match builder filters out markup-check rules whenever it's building a cascading match.** A minimal, local change — `cascading` is already a parameter at exactly that call site. No separate resolution path is needed for markup checks; they go through the exact same resolution, they just never survive into a cascading match. A markup rule in a policy that matches the *root* content type still resolves normally, which is the only place a markup check should ever appear per §4.2/§4.3's design.

**Secondary, lower-priority finding — the Applicable Checks display.** [Management API & UI](management-api-ui-spec.md)'s "Applicable Checks" service works at the whole-*policy* level for the backoffice display and never resolves individual rules to check instances, so it can't apply the same per-rule filter cheaply. A policy containing both a property rule and a markup rule, matched via cascading, would still display as applying at that nested location — including, misleadingly, its markup rule. This is a display-only inaccuracy (nothing actually runs there, per the real fix above), not a correctness bug, and is a Management API/UI concern to pick up when the UI work for markup checks happens, not a blocker for the contract split itself.

### 4.6 Migration of the two existing checks — mechanical

| File | Change |
|---|---|
| `src/Umbraco.Community.ContentChecks.Core/Checks/ContentCheckBase.cs` | Split into `ContentCheckMetadataBase<TConfig>` (everything except the `EvaluateAsync` proxy) + `PropertyContentCheckBase<TConfig>` (the proxy + abstract typed overload), per §4.1. |
| `src/Umbraco.Community.ContentChecks.Core/Checks/Examples/RequiredPropertyCheck.cs` | `: ContentCheckBase<RequiredPropertyCheckConfig>` → `: PropertyContentCheckBase<RequiredPropertyCheckConfig>`. Nothing else changes — its `EvaluateAsync` override signature is identical. |
| `src/Umbraco.Community.ContentChecks.AI/Checks/RubricContentCheck.cs` | Same one-word rename. Nothing else changes. |
| `src/Umbraco.Community.ContentChecks.Core/Checks/ContentCheckCollection.cs` | None — already keyed off `Alias`, available on the metadata-only interface. |
| `src/Umbraco.Community.ContentChecks.Core/Configuration/ContentChecksBuilderExtensions.cs` | None for discovery (attribute-based discovery still matches both branches via the shared metadata interface) — only the new `ContentCheckRunner` constructor wiring from §4.2. |
| `src/Umbraco.Community.ContentChecks.Core/Policies/ContentCheckResolutionService.cs` | The policy-match fix from §4.5. |
| `src/Umbraco.Community.ContentChecks.Core/Checks/ContentCheckRunner.cs` | Constructor gains `IContentCheckMarkupProvider`; the run loop gains the lazy-fetch threading and dispatch switch from §4.2/§4.4. |
| `tests/Umbraco.Test.ContentChecks.Unit/Checks/FakeContentCheck.cs` | `: IContentCheck` → `: IPropertyContentCheck`. Everything else in the file is already exactly that shape. |
| `tests/Umbraco.Test.ContentChecks.Unit/Checks/ContentCheckRunnerTests.cs` | Every test's setup needs a `Mock<IContentCheckMarkupProvider>` passed to the runner's constructor. A new `FakeMarkupContentCheck` test double (mirroring `FakeContentCheck`) is also needed to exercise the new dispatch branch and the lazy-fetch-sharing behavior specifically. |
| `tests/Umbraco.Test.ContentChecks.Unit/Checks/Examples/RequiredPropertyCheckTests.cs`, `RubricContentCheckTests.cs` | Confirmed no change needed — their test-double infrastructure constructs `IContentCheckInfrastructure` only, unaffected by the base-class split. |

No database/persistence migration — `IContentCheck` and its config schema are code-registered, never persisted.

### 4.7 Everything else this split touches — checked, not assumed

- **Guard ([Guard](guard-spec.md)) and Triggers ([Triggers](triggers-spec.md)): confirmed unaffected.** Neither references `IContentCheck` at all — both operate purely on `ContentCheckReportEntry`/`RequirementLevel`/`ContentCheckStatus`. This split doesn't touch either.
- **`ContentCheckEvaluationService`: confirmed unaffected, zero changes.** It builds one run request per target from that target's resolved policy matches and calls the unchanged runner per target. Because §4.5's fix means a markup rule only ever survives into the *root* target's matches, it rides along in that one request's rule list exactly like any other rule — "whole page only" falls out of §4.5's fix with no changes needed here.
- **`ContentCheckResult`/`ContentCheckTarget`: confirmed zero changes**, per §6's already-settled decision.
- **Management API: confirmed zero *required* changes.** The check-type response model is built entirely from the metadata surface (`Alias`/`Name`/`Description`/`Cost`/`Kind`/`ConfigSchema`), all of which stay on the thinned `IContentCheck`.
- **Open, recommended but not required:** the policy editor's rule picker currently has no way to know "is this check markup-based" to, say, grey it out when authoring a policy on a nested element type (where §4.5 means a markup rule would be accepted but silently never run there). A small flag on the check-type response model would let the UI do this, but it's a Management API/UI-layer decision belonging to whenever that UI work happens, not a blocker for building the contract split itself.

## 5. Granularity — **Settled**

Markup checks operate at **whole-page level only**. They do not participate in [Policy Resolution](policy-resolution-spec.md)'s cascading-into-nested-elements mechanism (a property-level/structural concept — walking Block List/Grid/Nested Content to discover nested element instances). Rendering one nested block's markup in isolation isn't a well-defined idea, and the real use cases named in §2 are inherently about the full page as delivered, not a fragment of it.

## 6. Result and Target shape — **Settled**

Markup checks report **page-level targets only** — no attempt to correlate a markup-level issue (a missing `alt`, say) back to a specific property.

Consequence: `ContentCheckTarget` needs **no changes at all**. The existing shape already supports "the whole item, no specific property" by leaving every field null (that was true of the original design — populated fields narrow the target, all-null means the whole item). A markup check just always leaves `PropertyAlias` null, optionally sets `Culture`/`Segment` for a specific language variant, and that's the entire story. Same `ContentCheckResult`, same `ContentCheckTarget`, no markup-specific output type anywhere.

Two named consequences of this choice, not problems to solve, just facts about what it costs:

- **The "click-through to the offending property" story bifurcates.** A property check's target can jump the editor to a specific field. A markup check's target has nowhere in the property editor to go — if it gets a click-through at all, it has to land somewhere else entirely (a rendered preview of the page), not a spot in the content form.
- **`Message` carries the entire localization burden for markup checks.** With no property to point at, "the image in the second paragraph is missing alt text" can only ever be prose the check writes, never something `ContentCheckTarget` expresses structurally. Real usability ceiling versus property checks, accepted as the cost of this choice.

## 7. State model: always current/draft, never published — **Settled**

Property checks are built on `IContent` (Umbraco's editorial/draft model), not `IPublishedContent` (the published-cache model). Markup checks always request a **preview render reflecting current draft state**, never a published-state render. This is one decision, not two, expressed in two different worlds (the DB model and the rendering model).

**Reasoning:** `IContent`'s current state already subsumes "audit my existing site" for the overwhelming majority of pages — for anything nobody's actively editing, current state already equals published state. The only place these diverge is a page with a pending, unapproved draft edit — and for that page, checking the draft is strictly *better* than checking what's published, because it catches the problem before it goes live instead of after. No real scenario was found where checking the published version instead of the current version leaves you better off.

**Named, accepted trade-off:** this doesn't serve an explicit "audit exactly what visitors are seeing right now, ignoring any pending drafts" use case. Considered and accepted as not a real loss, given the reasoning above — but recorded here as a conscious choice, not a silent gap.

**Practical effect:** the markup provider's request never needs a published-vs-preview fork. One mode, not two.

## 8. Retroactive validation

Rendering a full page is expensive compared to reading a property — a concrete, real example of "costly but not AI-judged." This validates keeping `Cost` and `Kind` as independent properties on `IContentCheck` rather than one collapsed enum, a decision that was made on a hypothetical (a slow third-party API check) before this thread gave it a second, concrete example.

## 9. The markup provider

### 9.1 Registration shape — **Settled**

A single, DI-replaceable `IContentCheckMarkupProvider` service — not a discovered collection like check types or element sources. Reasoning: most sites have exactly one rendering pipeline. A built-in default works until a site swaps it out, the same way replacing any other Umbraco service works.

**Named but not pursued:** a discovered collection, each entry declaring what it can handle (mirroring the element-source pattern, keyed by property-editor alias in that case), would serve a site mid-migration (part legacy Razor, part new headless) or a large site with genuinely different frontends per section. More flexible, costs more to build, for a rarer case. Not chosen against — just not the default.

### 9.2 Request/response shape — **Settled and built**

Built almost exactly as sketched — a decent signal the original sketch was already in the right place. No full `IContentCheckSnapshot`: a markup provider isn't reading properties, it just needs enough identity to know what to fetch.

```csharp
public sealed class ContentCheckMarkupRequest
{
    public required Guid ContentKey { get; init; }
    public string? Culture { get; init; }
    public string? Segment { get; init; }
}

public sealed class ContentCheckMarkupResult
{
    public required bool Success { get; init; }
    public string? Html { get; init; }
    public string? Url { get; init; }
    public int? StatusCode { get; init; }
    public string? FailureReason { get; init; }
}
```

Provider failure (no route, fetch error, non-2xx response) maps cleanly onto the **existing** `ContentCheckStatus.NotEvaluated` / `ContentCheckNotEvaluatedReason.Error` machinery already in [Core Contract](core-contract-spec.md) — no new mechanism needed, and no more specific reason code was needed in practice. Both types live in `Umbraco.Community.ContentChecks.Core.Markup`.

### 9.3 Implementation approach for the built-in default — **Settled and built: Option A**

**Option A — HTTP round-trip, built as `HttpContentCheckMarkupProvider`.** Resolves the item's URL via `IPublishedUrlProvider.GetUrl` (wrapped in `IUmbracoContextFactory.EnsureUmbracoContext()`, since this runs from a background check run, not a live request — there's no ambient `UmbracoContext` otherwise), mints a preview credential via §9.4's seam for the super user, and does a plain `HttpClient` GET with the credential's cookie(s) attached. A non-2xx response, a fetch exception, or an unroutable URL (`Constants.Routing.Unroutable`) all map to `Success = false` with a `FailureReason`; a cancelled fetch propagates rather than being reported as a failure (mirrors `RubricContentCheck`'s same choice for a cancelled AI judgment). Unit-tested against a mocked `HttpMessageHandler` — url-unroutable, credential-mint-failure, success, non-2xx, fetch-exception, and cancellation are each their own test.

**Option B — in-process rendering — dropped, not just deferred.** Would tie tightly to Umbraco's internal rendering APIs (route values, controller invocation) for no real benefit over Option A, and doesn't even apply to a headless frontend (there's nothing in Umbraco's own pipeline to render in-process when a separate app owns rendering) — so it was never going to be *the* general answer, only a possible optimization for one specific setup. Not worth the fragility for that.

**Known real costs, inherited from Option A's original write-up, not yet mitigated:** no base-URL override for load-balanced/reverse-proxied environments yet (§10, still open — the current build just calls Umbraco's own URL resolution with no override point), and no explicit fetch timeout/circuit-breaking (`HttpClient`'s default timeout applies; a slow render can still hold up a check run longer than ideal).

**Not yet proven against a real running site.** Getting a URL at all for *any* content item (even one genuinely published) couldn't be made to work in this project's lightweight SQLite integration-test harness, despite trying the documented fixes (`EnsureUmbracoContext()`, `WithApplication = true`, `PublishedRepositoryEvents = true`, and manually driving `IDocumentUrlService.InitAsync`) — the published-content/navigation cache (a separate subsystem from the URL-segment tracker, which *did* work once initialized) never got populated in that harness. This is standard, universally-relied-on Umbraco behavior on a real site — resolving a content item's own URL is how every Umbraco page gets routed at all — so it's trusted here, not test-proven in this repo. Re-verify against the real demo site once there's a convenient way to create content through it end-to-end.

**Shelved idea — configurable URL template.** A single template (`https://.../preview?key={key}&culture={culture}`) could plausibly cover both a traditional site and many headless setups (point it at the frontend's own render endpoint instead) with one mechanism, leaving a fully custom `IContentCheckMarkupProvider` only for the genuinely bespoke cases (non-HTTP transport, GraphQL, custom auth). **Explicitly left out for now, for a real reason, not an excuse:** a flat template can't know which domain to use for a given content item without replicating Umbraco's own domain resolution — Umbraco's Culture and Hostnames feature can map different domains to different content roots or cultures within one instance, and a template string alone can't pick the right one. Recorded here so it isn't silently forgotten if domain-aware templating becomes worth the effort later.

### 9.4 Preview credential seam — **Settled and built, for v18**

Proven empirically (2026-11-13), not just theorized: Umbraco 18's front-end preview mode can be entered entirely from code, for a known, already-*saved* draft, with no real browser session or interactive editor. This covers the AI-generation-loop's baseline case (agent saves a draft the ordinary way, checks it before publish) without needing any of the harder machinery described below.

**The mechanism (Umbraco 18.x):** preview is gated by a single cookie, `UMB_PREVIEW`, whose value is an opaque token built from `{userKey}|{securityStamp}`. `Umbraco.Cms.Core.Preview.IPreviewTokenGenerator.GenerateTokenAsync(Guid userKey)` mints that token for any user key, synchronously and with no `HttpContext` — a background process can call it directly. `PreviewAuthenticationMiddleware` later verifies the same cookie via `VerifyAsync` and puts the request into preview mode. The interface is marked `[Obsolete("...will be removed in Umbraco 19.")]`, but fully functional for this branch's whole lifetime (this repo never upgrades `v18/dev` into v19 — see [Overview](overview.md) §1's branch model).

**Built:** `Umbraco.Community.ContentChecks.Core.Markup.IContentCheckPreviewCredentialMinter`, with a v18 implementation (`PreviewTokenCredentialMinter`) wrapping `IPreviewTokenGenerator`. Takes a "judge" user key, returns the cookie(s) to attach to the markup provider's outbound fetch — deliberately its own seam rather than inlined into the (not yet built) provider, because the real mechanism is different and heavier on Umbraco 19 (below). Proven against a real, EF-backed seeded user and the real `IPreviewTokenGenerator`: `PreviewTokenCredentialMinterIntegrationTests` mints a token for `Constants.Security.SuperUserKey` and confirms Umbraco's own `VerifyAsync` accepts it and resolves back to the same user — not a mock standing in for Umbraco's own verification.

**Umbraco 19 (checked against 19.0.0-rc1, not yet built here):** `IPreviewTokenGenerator` is gone entirely. `PreviewAuthenticationMiddleware` instead re-authenticates via the real `"UmbracoBackOffice"` cookie-auth scheme (`context.AuthenticateAsync(...)`), and a new `IPreviewAccessEvaluator.CanPreviewDocument` check means the chosen judge identity must actually have browse/start-node permission over the content being checked, or the request silently falls back to the published version instead of warning. The headless-mintable primitive still exists though: `ICoreBackOfficeSignInManager.CreateUserPrincipalAsync(Guid userKey)` builds the same claims principal as before — the only change is the last step, a real `HttpContext.SignInAsync("UmbracoBackOffice", principal)` instead of a disposable token. **Recommendation for when a `v19/dev` branch exists:** give the judge user an Admin-group account so the permission check always passes regardless of which node is checked, and implement a second `IContentCheckPreviewCredentialMinter` behind the same interface — the seam above exists specifically so this is a one-file swap, not a provider redesign.

**Relationship to the sibling Draft Sets project, named precisely because the two problems are not the same size:** Draft Sets already investigated adjacent territory — decorating `IPublishedContentCache`, a `PublishedContentWrapped` overlay, an ambient accessor mirroring `HybridAccessorBase` — but that was for rendering **in-memory, not-yet-persisted** edits (content captured ambiently from an open Draft Set that was never saved to the DB), which they found clean only for value edits and blocked on structural changes. The markup provider's baseline case is narrower: a real, saved draft that Umbraco's own preview mechanism already knows how to render, no overlay needed. Reconciling with actual in-memory Draft Set edits (the harder problem) stays [Overview](overview.md)'s Phase 7 concern, not something this seam needs to solve.

**Not yet proven:** that the minted cookie, attached to a real outbound HTTP request against a running site (rather than verified directly via `VerifyAsync` in-process), actually renders the draft's Razor view end-to-end. An early throwaway spike against a stripped-down host saw a 404 on the rendered page even with `InPreviewMode` confirmed true — plausibly an artifact of that spike's deliberately incomplete host setup (no ModelsBuilder/Razor-compile settings matching a real site), not a real limitation, but unconfirmed against this repo's actual demo site. Re-test once the markup provider's HTTP-fetch piece (§9.3) is built.

## 10. URL/domain resolution — **Open fork; not yet built**

The current `HttpContentCheckMarkupProvider` (§9.3) calls `IPublishedUrlProvider.GetUrl` directly with no override point at all — fine for a normal single-domain site, but doesn't yet solve the problem below.

For Option A (HTTP round-trip) specifically: support an explicit override setting for load-balanced/reverse-proxied environments where the site can't reliably auto-detect its own public URL from the current request, falling back to best-guess otherwise.

**To confirm before building:** Umbraco Core likely already has an equivalent setting/service for exactly this problem (an application-URL override with its own fallback logic), used internally for things like building absolute links. Not verified against current Umbraco source in this conversation — needs checking when this actually gets built, not asserted as fact now.

**The fork, unresolved:** reuse Umbraco's existing setting/service directly (zero new config), or define a Content Checks-specific override that falls back to Umbraco's setting when unset. The second buys an escape hatch for wanting check-fetching to hit a different address than user-facing links do (an internal network address, say) without forcing redundant configuration for the common case where one URL is fine for both purposes.

## 11. Secondary considerations, named but not developed

- ~~**Fetch-level caching within one run.**~~ — **resolved, see §4.2.** The runner fetches markup once per run (lazily, only if a markup rule actually executes) and shares it across every markup check in that run, distinct from [Core Contract](core-contract-spec.md)'s cross-run result-cache.

## 12. Open questions, consolidated

1. ~~Exact shape of `ContentCheckMarkupContext` / `ContentCheckMarkupRequest`~~ — **resolved and built, see §9.2 and design-settled in §4.3.** `IMarkupContentCheck`/`MarkupContentCheckBase` themselves (§4's check-type contract split) are design-settled (2026-11-13) but not yet implemented — see §4 for the full settled shape, awaiting the actual code change.
2. ~~HTTP round-trip vs in-process rendering~~ — **resolved: Option A (HTTP), built as `HttpContentCheckMarkupProvider`, see §9.3.** Option B dropped outright, not deferred. The domain-aware-template idea from §9.3's original write-up remains shelved, unaffected by this.
3. Whether to reuse Umbraco's own application-URL setting directly or define a Content Checks-specific override with fallback (§10) — still open; the built provider has no override point yet at all.
4. Whether the single-provider registration shape (§9.1) ever needs to become a discovered collection — revisit only if a real multi-frontend/migration case shows up.
5. Whether provider failure ever needs a more specific `NotEvaluatedReason` than the existing generic `Error` (§9.2) — not needed in practice; `FailureReason`'s free text has covered every case built so far.
6. ~~How to get a preview-mode render for an already-saved draft with no real editor session~~ — **resolved and built for v18, see §9.4.** Who configures which backoffice user acts as "the judge" is also now settled, pragmatically rather than via a new setting: the built provider hard-codes `Constants.Security.SuperUserKey`, see §9.3's remarks on `HttpContentCheckMarkupProvider`. Revisit only once something else in the project needs user-configurable settings too.
7. Whether the minted preview cookie actually renders a draft's Razor view end-to-end over a real HTTP fetch — **still open.** Proven only as far as Umbraco's own token verification (§9.4) and the provider's own unit tests (§9.3, which mock the HTTP layer entirely); not yet against a real running site. §9.3's "Not yet proven" note covers the same gap for URL resolution itself, which blocks testing this one too.
8. ~~Exact shape of the `IContentCheck`/`IPropertyContentCheck`/`IMarkupContentCheck` split, who calls the markup provider, and how Policy Resolution handles a markup rule~~ — **design-settled 2026-11-13, see §4.** A real bug was caught in the process: without §4.5's fix, a markup rule would wrongly resolve as a cascading match into nested elements. Not yet implemented.
9. The policy editor's rule picker has no way to show "this check only applies to the whole page" (§4.7) — open, Management API/UI concern, not a blocker.
10. The Applicable Checks display would misleadingly show a markup rule as applying at a cascaded nested location, even though it would never actually run there (§4.5's secondary finding) — open, same UI-layer bucket as #9.

## 13. What this thread reused from Core Contract, unchanged

Worth naming because it's a decent signal [Core Contract](core-contract-spec.md)'s contract was drawn in the right place: `ContentCheckResult`, `ContentCheckStatus`, `ContentCheckTarget`, and the `NotEvaluated`/`Error` machinery all carried over to markup checking with zero modification. Only the *input* side (a new pair of interfaces, a new context type) needed anything new.
