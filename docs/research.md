# Market & Ecosystem Research (2026-08-05)

**Part of:** [Overview](overview.md). Four parallel research passes, run before committing to the architecture in [Core Contract](core-contract-spec.md) and [Policy Resolution](policy-resolution-spec.md). Findings below; implications are cross-referenced into the other specs where they changed a decision.

---

## 1. Umbraco Marketplace — direct competitors & naming collision check

**No exact naming collision.** Nothing named "ContentGuards," "Content Guards," "ContentChecks," or "Content Checks" exists on NuGet or GitHub as of this research.

**Adjacent "\*Guard\*" packages** (the reason the name changed — see [Overview](overview.md) §1): `Cogworks.ContentGuard` (locks a page while editing, Umbraco v8, unmaintained), `Umbraco.Community.ContentLock` (modern fork of the above, active), `Umbraco-Community-PropertyGuard` (restricts who can edit a property, brand new, .NET 10). None do content-quality checks.

**Closest real competitors:**

| Package | What it does | Dev-extensible | Editor-authorable | AI-judged |
|---|---|---|---|---|
| [Preflight](https://github.com/nathanwoulfe/preflight) | Readability, sentence/word length, broken links, text replacement, variant-aware | Yes — `IPreflightPlugin` | Partial — toggle/tune existing checks, thresholds, scope, block-on-save; **cannot author a new check type** | No |
| [ProWorks Umbraco.AI Page Evaluator](https://github.com/ProWorksCorporation/ProWorks-Umbraco.AI-Page-Evaluator) | AI-judged page review: clarity, SEO/AEO/accessibility, brand voice | Open source, thin docs | Yes — pick doc type + AI profile + write/generate the prompt | **Yes** — built on Umbraco.AI directly |
| [Community.Umbraco.Checklist](https://github.com/JohanReitsma83/Community.Umbraco.Checklist) | Approval-style checklist tasks | Yes, via a `checklist.json` file (dev/config) | No | No |
| [SEO Checker](https://marketplace.umbraco.com/package/seochecker) | Missing meta tags, broken links, snippet preview | Not documented | No, fixed rule set | No |
| [RankOne SEO Toolkit](https://github.com/markwemekamp/RankOne-Umbraco-SEO-Tool) | SEO score dashboard | Some | No | No |

**Preflight, checked directly (2026-08-05):** last release `13.0.0` (Feb 2024), last commit May 2024 — over two years stale, no release for Umbraco 14+. Umbraco 14 replaced the entire backoffice UI (AngularJS → the current Lit/TypeScript stack); Preflight was built for the old one and cannot run on v17/v18 without a rewrite nobody has done. It is not a live competitor to this project — it's prior art confirming the "types in code, settings in a UI" split ([Core Contract](core-contract-spec.md) §3, the config schema system), not a product currently usable on the CMS versions this project targets. It also confirms, at its peak, editors could tune existing checks but never author a genuinely new one — nothing has done that yet on any current Umbraco version.

**Takeaway:** ProWorks' Page Evaluator is the one project already doing "AI judges my content" on Umbraco.AI — noted as a risk (see [Overview](overview.md)) and a thing to review before [AI Integration](ai-integration-spec.md), not a blocker. Nobody offers a general extensible pass/warn/fail *framework* spanning dev-defined types and editor-authored sets, deterministic or AI-judged. That gap is confirmed open.

## 2. Umbraco Health Checks — the extension model this project copies

Confirmed against docs.umbraco.com and CMS source:

- Contract: `[HealthCheck(Guid, Name, Description, Group)]` attribute + `HealthCheck`/`AbstractSettingsCheck` base class, `GetStatus()` returns `HealthCheckStatus` (message + `ResultType`: Success/Error/Warning/Info + optional `Actions`).
- **Fix-action mechanism is real and first-class**: a `HealthCheckStatus` can carry a `HealthCheckAction`; the dashboard shows a "Fix" button; clicking it calls `ExecuteAction(action)`. **Considered for the contract, then deliberately rejected** (see [Core Contract](core-contract-spec.md) §1): a Health Check fix mutates a config value or a file — unversioned, unpermissioned, low-stakes. A content-property fix is a governed content edit and has no business being a validation plugin's job. Recorded here so the idea isn't rediscovered and re-added without the reasoning that killed it.
- **Known defect, noted but no longer applicable:** [GitHub issue #5110](https://github.com/umbraco/Umbraco-CMS/issues/5110) — the fix-action's custom-input field is hardcoded to email-format validation everywhere, breaking any fix action that needs a non-email value. Moot for ContentChecks now that there's no fix-action UI at all, but the general lesson generalizes to the config schema itself: never hardcode an input's validation outside the schema that owns it.
- Real-world examples ([Our.Umbraco.HealthChecks](https://github.com/prjseal/Our.Umbraco.HealthChecks), [nickoakes/custom-umbraco-health-checks](https://github.com/nickoakes/custom-umbraco-health-checks), [Stephen Garside's uptime check](https://www.stephengarside.co.uk/blog/custom-health-checks-for-umbraco-8-website/)) are uniformly dev-only/hardcoded — none expose per-check settings to a non-developer. **Preflight exists as a separate, parallel package specifically because Health Checks don't fit per-content, editor-configurable settings** — strong direct evidence for building our own editor-facing settings from day one rather than assuming Health Checks' model covers it out of the box.

## 3. SEO / editorial-quality packages — pricing & configurability landscape

| Package | Checks | Editor-configurable | Pricing |
|---|---|---|---|
| [SeoToolkit](https://github.com/patrickdemooij9/SeoToolkit.Umbraco) | Meta fields, sitemap, robots.txt, audit | Partial (settings, not rule logic) | Free/OSS |
| [SEO Checker](https://soetemansoftware.nl/seochecker) | Meta tags, broken links, canonical, keywords | No | €249–€1,125 |
| [Textmetrics](https://www.textmetrics.com/textmetrics-launches-the-umbraco-cms-plugin) | Live SEO/readability scoring | No (dev sets up data type/corporate style) | Free tier + paid |
| [Preflight](https://github.com/nathanwoulfe/preflight) | Readability, links, forbidden words | Yes, partial (see §1 above) | Free/OSS |
| [Clip](https://github.com/nathanwoulfe/Clip) | Content-creation governance (doc-type/quota limits) | **Yes, fully, no code** | Free/OSS |
| [Umbraco Workflow](https://umbraco.com/products/add-ons/workflow/) | Multi-stage approval | Stages/groups, not content rules | ~€2,800/yr |
| [Accessibility Reporter](https://umbraco.accessibilityreporter.com/) | WCAG compliance | No, fixed ruleset | Free/OSS |

**Takeaway:** every SEO tool found is vendor-fixed. Clip proves a pure no-code rules UI is buildable and shippable in this ecosystem (for a simpler governance domain); nothing combines that with content-quality scoring. Confirms the gap stated in the [Overview](overview.md).

## 4. Cross-CMS flexibility patterns

Researched Contentful, Optimizely, Sanity, WordPress (Yoast/Rank Math), Drupal. Patterns adopted or explicitly deferred:

- **Adopted** ([Core Contract](core-contract-spec.md), config schema): Drupal's Field Validation module — developer registers a validator type in code, editor picks it from a list and fills a generated config form. Directly validates the schema-driven config design.
- **Adopted** ([Core Contract](core-contract-spec.md), result shape): Sanity's severity-per-rule idea (error vs warning, not just pass/fail) — already covered by the 5-state `ContentCheckStatus`, ahead of what any researched platform does today.
- **Adopted** ([Triggers](triggers-spec.md)): the lesson from Contentful's `setInvalid()` — a cosmetic-only "invalid" flag that doesn't block publish is worse than no check, because it looks like protection and isn't. Every blocking check must wire into the real publish pipeline.
- **Adopted** ([Core Contract](core-contract-spec.md), registration): Yoast/Rank Math's self-registering check pipeline — new check types plug into one central "run all checks" pass rather than needing bespoke wiring, which is already how the collection-builder registration works.
- **Deferred, flagged as open question 6** (see [Overview](overview.md)): Drupal's escape-hatch "Callback"/"Expression" validator for power users who need something a no-code form can't express. Not built in v1; the contract must not preclude adding it later.
- **Notably absent everywhere researched:** a named, reusable, editor-typed rubric field for LLM-judged criteria. No platform researched offers this today — it isn't a solved pattern to copy, it's new ground this package is actually breaking.
