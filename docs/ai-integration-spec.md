# AI Integration — `Umbraco.Community.ContentChecks.AI`

**Status:** First real check built and unit-tested (2026-11-13) — `RubricContentCheck`, a free-text rubric judged via Umbraco.AI's Decision API, plus the agent tools and a hard round cap. See the build checklist below for what's done versus still open.
**Part of:** [Overview](overview.md). Implements [Core Contract](core-contract-spec.md)'s `IContentCheck` for the AI-judged tier; doesn't change the contract itself.

---

## Packaging

A project **inside this repo**, not inside the Umbraco.AI monorepo (see [Overview](overview.md) §1). It project-references `Core` (for the contract + ported config infra) and takes `Umbraco.AI` as an ordinary NuGet package reference (version range, same as any third-party consumer) for chat/profile access.

**Packaging call:** ship this as part of the same single-repo release rather than standing up a second product's worth of versioning/CI/changelog tooling. One `version.json` for the whole repo, one release pipeline, two NuGet packages out of it (`Umbraco.Community.ContentChecks`, `Umbraco.Community.ContentChecks.AI`). Multi-product tooling (per-product `changelog.config.json`, `release-manifest.json` include/exclude) is Umbraco.AI-monorepo-scale machinery this repo doesn't need at its size — revisit only if this repo grows a third package.

## Before building this

**Review `ProWorks-Umbraco.AI-Page-Evaluator` directly** (see [research.md](research.md)) — an existing open-source project already doing AI-judged content review on top of Umbraco.AI (editors pick a doc type + AI profile + write/generate a prompt; scores clarity/SEO/brand-voice). It is scoped to page-level evaluation via prompt customization, not a general pluggable check-type framework, so it doesn't overlap with this project's architecture — but it may already have solved UI/prompt problems worth reusing rather than re-deriving, and its existence means this space has at least one other active author to be aware of (see risk in the [Overview](overview.md)).

## Build checklist

- [x] Real AI-judged check: `RubricContentCheck` (`Cost.Metered` + `Kind.Nondeterministic`), judged via Umbraco.AI's experimental Decision API (`IAIDecisionService.AskAsync`) rather than a hand-rolled chat prompt — fixed `TrueCriteria`/`FalseCriteria`, the admin's rubric only ever appears in `Instructions`, content passed as `state`. Returns a calibrated pass probability (`Score`) and a `Pass`/`Fail` verdict against the configured `Threshold`. **Deviates from the original plan here:** the Decision API returns a probability, not free text, so there's no per-target judge reasoning to surface — the `Message` is a generic score-vs-threshold line, not itemized feedback.
- [x] **Rubric authoring UX, settled: free-form text.** Resolves [open question 1](overview.md) — `RubricContentCheckConfig.Rubric` is a plain multi-line text field, no guided/weighted-criteria form.
- [x] **The judge profile is configured separately from any generating profile** (finding G1) — `ProfileId` is its own optional field (`null` uses the default decision profile), and the field description tells the admin to use a different profile than whatever generated the content. **Not yet built:** no UI warning fires when the judge and generating profiles actually do resolve to the same one.
- [x] ~~Structured output via a simplified schema + server-side transform~~ — **superseded.** Using the Decision API directly (a calibrated probability) sidesteps the strict-mode schema problem entirely; no simplified-schema transform was needed.
- [x] Provider failure, timeout, and any other exception map onto `NotEvaluated` (`ContentCheckNotEvaluatedReason.Error`) — never `Fail`. A cancelled request (`OperationCanceledException`) propagates instead of being reported as a verdict, matching Umbraco.AI's own Decision evaluators.
- [x] Agent tools: `RunContentChecksTool` (`run_content_checks`) and `ListContentCheckPoliciesTool` (`list_content_check_policies`), in `Tools/`.
- [x] **Loop discipline in the tool layer (finding G2), settled: the tool enforces a hard round cap itself.** Resolves [open question 2](overview.md). `InMemoryContentCheckAgentRoundTracker` enforces a hard `MaxRounds = 5` (process-lifetime counter, not persisted — deliberately not configurable yet). `ContentCheckAgentRoundStatus` reports the round number and cap back to the caller.
- [ ] **Ship default rubrics** (finding K): tone-of-voice, readability, SEO value. Still a blank text box today — `RubricContentCheckConfig.Rubric` defaults to `""`, no bundled library.
- [x] Delete [Core Contract](core-contract-spec.md)'s throwaway proving check B (`StubRubricCheck`, in `Core/Checks/Examples/`) — done. `ContentCheckConfigSchemaBuilderTests` no longer depends on it either; its one test needing a config with a non-default property initializer now uses a small test-local type instead.

## Future idea, deliberately not v1: AI-assisted rubric/rule authoring

Raised as a possible replacement for [open question 6](overview.md)'s power-user escape hatch (a sandboxed expression/"Script Check" type), not an addition alongside it: instead of an advanced user writing a raw expression for something a no-code form can't express, they write a plain-English description of what they want checked, and an AI-assisted authoring flow turns that into either a rubric (trivial, since rubric config is already free text per the decision above — the description more or less *is* the rubric) or, more ambitiously, an assembled set of existing deterministic check configs (a genuinely new "compile an English policy description into check rules" capability, not just filling in one field). **Keep as a future option — do not build for v1.** Recorded here rather than in the escape-hatch open question itself, because it's a different kind of thing: the Script Check idea is a new *check-authoring capability* (a check type that runs arbitrary logic); this is a new *authoring-assist* feature on top of check types that already exist, and conflating the two would blur why the escape hatch is deferred versus why this is. If this ever gets built, it doesn't change [Core Contract](core-contract-spec.md) at all — an AI that writes a `TConfig` for an existing check type is an authoring-time tool, not a new runtime contract shape.
