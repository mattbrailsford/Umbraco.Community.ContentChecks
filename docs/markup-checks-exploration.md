# Content Checks — Markup-Based Checking (Exploration)

**Status:** Exploratory. This captures the full extent of an idea, not a build-ready spec. Some things got settled along the way (marked **Settled**); most of the interesting parts are still genuinely open (marked **Open**) and are recorded as forks with their reasoning, not as decisions. Narrowing to what actually ships is a deliberate, separate, later step — nothing here should be read as "cut" or "deferred" just because it's unresolved. **One piece of groundwork is now actually built, ahead of the rest of this (2026-11-13):** §9.4's preview-credential seam (`IContentCheckPreviewCredentialMinter`), proven against real Umbraco 18 code, not just sketched. The full `IContentCheckMarkupProvider`/`IMarkupContentCheck` contract itself (§4, §9) remains unbuilt.

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

## 4. Contract shape

**Open, but converging.** `IContentCheckSnapshot` is a property bag; markup is a raw HTML string. These are different input shapes, not the same shape stretched to fit, so property checks and markup checks shouldn't share one `EvaluateAsync` signature. The shape explored:

```csharp
// Pure metadata — no EvaluateAsync. What every check declares about itself, regardless of shape.
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
    Task<ContentCheckResult> EvaluateAsync(ContentCheckContext context, ContentCheckConfig config, CancellationToken ct);
}

public interface IMarkupContentCheck : IContentCheck
{
    Task<ContentCheckResult> EvaluateAsync(ContentCheckMarkupContext context, ContentCheckConfig config, CancellationToken ct);
}
```

This is a **mandatory branch, not an optional add-on** — a meaningfully different relationship than `IFixableContentCheck` (considered and rejected in [Core Contract](core-contract-spec.md)), which would have been additive on top of an always-runnable base. Here, `IContentCheck` alone describes something but can't run anything; you have to be one of the two children to be runnable. Anything that actually executes a check (not just lists it) has to pattern-match which one it's holding.

Base classes mirror the split, with a third, more foundational shared piece underneath both — the attribute-reading/lazy-schema/`ResolveConfig` plumbing has nothing to do with property-vs-markup either:

```csharp
internal abstract class ContentCheckMetadataBase<TConfig> : IContentCheck { /* attribute + schema + ResolveConfig */ }
public abstract class PropertyContentCheckBase<TConfig> : ContentCheckMetadataBase<TConfig>, IPropertyContentCheck { ... }
public abstract class MarkupContentCheckBase<TConfig> : ContentCheckMetadataBase<TConfig>, IMarkupContentCheck { ... }
```

**Open:** exact members of `ContentCheckMarkupContext` beyond "the HTML" — see §7.

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

### 9.2 Request/response shape — **Open, sketched**

The request probably does **not** need the full `IContentCheckSnapshot` abstraction — that interface exists for reading properties, and a markup provider isn't reading properties, just needs enough identity to know what to render (content Key, maybe a pre-resolved URL) plus culture/segment.

```csharp
public sealed class ContentCheckMarkupRequest
{
    // Key + culture/segment at minimum; exact shape still open — see also §7 (no Preview flag needed).
}

public sealed class ContentCheckMarkupResult
{
    public string? Html { get; init; }
    public string? Url { get; init; }
    public int? StatusCode { get; init; }
    public bool Success { get; init; }
    public string? FailureReason { get; init; }
}
```

Provider failure (site down, timeout, no provider configured) maps cleanly onto the **existing** `ContentCheckStatus.NotEvaluated` / `ContentCheckNotEvaluatedReason.Error` machinery already in [Core Contract](core-contract-spec.md) — no new mechanism needed. A more specific reason code (e.g. distinguishing "markup unavailable" from a generic error) is a possible, not-decided future refinement.

### 9.3 Implementation approach for the built-in default — **Open, two real options, not converging yet**

**Option A — HTTP round-trip.** The provider resolves the item's URL and makes an HTTP GET to it. Simple, robust, indifferent to Umbraco's internal rendering plumbing. Real costs: needs to reliably know its own base URL (awkward behind load balancers — see §10), needs to somehow carry preview auth over the request (Umbraco preview is normally tied to a signed-in backoffice user's cookie, not something a background process naturally has), needs real timeout/circuit-breaking so a slow render doesn't hang a check run, and has a faint self-referential-request oddity (a site calling itself over the network).

**Option B — in-process rendering** via Umbraco's own pipeline directly. No network hop, and it's easier to construct the right preview context *deliberately* rather than fake it over HTTP. Costs: ties much more tightly to Umbraco's internal rendering APIs (route values, controller invocation), which are more fragile and version-sensitive than a plain HTTP call.

**Shelved idea — configurable URL template.** A single template (`https://.../preview?key={key}&culture={culture}`) could plausibly cover both a traditional site and many headless setups (point it at the frontend's own render endpoint instead) with one mechanism, leaving a fully custom `IContentCheckMarkupProvider` only for the genuinely bespoke cases (non-HTTP transport, GraphQL, custom auth). **Explicitly left out for now, for a real reason, not an excuse:** a flat template can't know which domain to use for a given content item without replicating Umbraco's own domain resolution — Umbraco's Culture and Hostnames feature can map different domains to different content roots or cultures within one instance, and a template string alone can't pick the right one. Recorded here so it isn't silently forgotten if domain-aware templating becomes worth the effort later.

### 9.4 Preview credential seam — **Settled and built, for v18**

Proven empirically (2026-11-13), not just theorized: Umbraco 18's front-end preview mode can be entered entirely from code, for a known, already-*saved* draft, with no real browser session or interactive editor. This covers the AI-generation-loop's baseline case (agent saves a draft the ordinary way, checks it before publish) without needing any of the harder machinery described below.

**The mechanism (Umbraco 18.x):** preview is gated by a single cookie, `UMB_PREVIEW`, whose value is an opaque token built from `{userKey}|{securityStamp}`. `Umbraco.Cms.Core.Preview.IPreviewTokenGenerator.GenerateTokenAsync(Guid userKey)` mints that token for any user key, synchronously and with no `HttpContext` — a background process can call it directly. `PreviewAuthenticationMiddleware` later verifies the same cookie via `VerifyAsync` and puts the request into preview mode. The interface is marked `[Obsolete("...will be removed in Umbraco 19.")]`, but fully functional for this branch's whole lifetime (this repo never upgrades `v18/dev` into v19 — see [Overview](overview.md) §1's branch model).

**Built:** `Umbraco.Community.ContentChecks.Core.Markup.IContentCheckPreviewCredentialMinter`, with a v18 implementation (`PreviewTokenCredentialMinter`) wrapping `IPreviewTokenGenerator`. Takes a "judge" user key, returns the cookie(s) to attach to the markup provider's outbound fetch — deliberately its own seam rather than inlined into the (not yet built) provider, because the real mechanism is different and heavier on Umbraco 19 (below). Proven against a real, EF-backed seeded user and the real `IPreviewTokenGenerator`: `PreviewTokenCredentialMinterIntegrationTests` mints a token for `Constants.Security.SuperUserKey` and confirms Umbraco's own `VerifyAsync` accepts it and resolves back to the same user — not a mock standing in for Umbraco's own verification.

**Umbraco 19 (checked against 19.0.0-rc1, not yet built here):** `IPreviewTokenGenerator` is gone entirely. `PreviewAuthenticationMiddleware` instead re-authenticates via the real `"UmbracoBackOffice"` cookie-auth scheme (`context.AuthenticateAsync(...)`), and a new `IPreviewAccessEvaluator.CanPreviewDocument` check means the chosen judge identity must actually have browse/start-node permission over the content being checked, or the request silently falls back to the published version instead of warning. The headless-mintable primitive still exists though: `ICoreBackOfficeSignInManager.CreateUserPrincipalAsync(Guid userKey)` builds the same claims principal as before — the only change is the last step, a real `HttpContext.SignInAsync("UmbracoBackOffice", principal)` instead of a disposable token. **Recommendation for when a `v19/dev` branch exists:** give the judge user an Admin-group account so the permission check always passes regardless of which node is checked, and implement a second `IContentCheckPreviewCredentialMinter` behind the same interface — the seam above exists specifically so this is a one-file swap, not a provider redesign.

**Relationship to the sibling Draft Sets project, named precisely because the two problems are not the same size:** Draft Sets already investigated adjacent territory — decorating `IPublishedContentCache`, a `PublishedContentWrapped` overlay, an ambient accessor mirroring `HybridAccessorBase` — but that was for rendering **in-memory, not-yet-persisted** edits (content captured ambiently from an open Draft Set that was never saved to the DB), which they found clean only for value edits and blocked on structural changes. The markup provider's baseline case is narrower: a real, saved draft that Umbraco's own preview mechanism already knows how to render, no overlay needed. Reconciling with actual in-memory Draft Set edits (the harder problem) stays [Overview](overview.md)'s Phase 7 concern, not something this seam needs to solve.

**Not yet proven:** that the minted cookie, attached to a real outbound HTTP request against a running site (rather than verified directly via `VerifyAsync` in-process), actually renders the draft's Razor view end-to-end. An early throwaway spike against a stripped-down host saw a 404 on the rendered page even with `InPreviewMode` confirmed true — plausibly an artifact of that spike's deliberately incomplete host setup (no ModelsBuilder/Razor-compile settings matching a real site), not a real limitation, but unconfirmed against this repo's actual demo site. Re-test once the markup provider's HTTP-fetch piece (§9.3) is built.

## 10. URL/domain resolution — **Open fork**

For Option A (HTTP round-trip) specifically: support an explicit override setting for load-balanced/reverse-proxied environments where the site can't reliably auto-detect its own public URL from the current request, falling back to best-guess otherwise.

**To confirm before building:** Umbraco Core likely already has an equivalent setting/service for exactly this problem (an application-URL override with its own fallback logic), used internally for things like building absolute links. Not verified against current Umbraco source in this conversation — needs checking when this actually gets built, not asserted as fact now.

**The fork, unresolved:** reuse Umbraco's existing setting/service directly (zero new config), or define a Content Checks-specific override that falls back to Umbraco's setting when unset. The second buys an escape hatch for wanting check-fetching to hit a different address than user-facing links do (an internal network address, say) without forcing redundant configuration for the common case where one URL is fine for both purposes.

## 11. Secondary considerations, named but not developed

- **Fetch-level caching within one run.** If several markup checks run against the same page in one check run, fetching the markup once and sharing it is an obvious optimization. Distinct from [Core Contract](core-contract-spec.md)'s result-cache — that's about not re-running a check whose *verdict* would be unchanged; this is about not re-fetching the same *raw markup* multiple times in one run.

## 12. Open questions, consolidated

1. Exact shape of `ContentCheckMarkupContext` / `ContentCheckMarkupRequest` beyond content identity + culture/segment (§4, §9.2).
2. HTTP round-trip vs in-process rendering vs (later, maybe) a domain-aware template, for the built-in default provider (§9.3).
3. Whether to reuse Umbraco's own application-URL setting directly or define a Content Checks-specific override with fallback (§10).
4. Whether the single-provider registration shape (§9.1) ever needs to become a discovered collection — revisit only if a real multi-frontend/migration case shows up.
5. Whether provider failure ever needs a more specific `NotEvaluatedReason` than the existing generic `Error` (§9.2).
6. ~~How to get a preview-mode render for an already-saved draft with no real editor session~~ — **resolved and built for v18, see §9.4.** Who configures which backoffice user acts as "the judge" (a dedicated setting, vs. always defaulting to the super user) is still open — deferred until the provider itself is designed, since it's the provider's caller that will own that decision.
7. Whether the minted preview cookie actually renders a draft's Razor view end-to-end over a real HTTP fetch — proven only as far as Umbraco's own token verification, not yet against a real running site (§9.4's "Not yet proven" note).

## 13. What this thread reused from Core Contract, unchanged

Worth naming because it's a decent signal [Core Contract](core-contract-spec.md)'s contract was drawn in the right place: `ContentCheckResult`, `ContentCheckStatus`, `ContentCheckTarget`, and the `NotEvaluated`/`Error` machinery all carried over to markup checking with zero modification. Only the *input* side (a new pair of interfaces, a new context type) needed anything new.
