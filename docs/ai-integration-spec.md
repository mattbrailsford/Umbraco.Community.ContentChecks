# AI Integration — `Umbraco.Community.ContentChecks.AI`

**Status:** Build sketch only (Phase 5).
**Part of:** [Overview](overview.md). Implements [Core Contract](core-contract-spec.md)'s `IContentCheck` for the AI-judged tier; doesn't change the contract itself.

---

## Packaging

A project **inside this repo**, not inside the Umbraco.AI monorepo (see [Overview](overview.md) §1). It project-references `Core` (for the contract + ported config infra) and takes `Umbraco.AI` as an ordinary NuGet package reference (version range, same as any third-party consumer) for chat/profile access.

**Packaging call:** ship this as part of the same single-repo release rather than standing up a second product's worth of versioning/CI/changelog tooling. One `version.json` for the whole repo, one release pipeline, two NuGet packages out of it (`Umbraco.Community.ContentChecks`, `Umbraco.Community.ContentChecks.AI`). Multi-product tooling (per-product `changelog.config.json`, `release-manifest.json` include/exclude) is Umbraco.AI-monorepo-scale machinery this repo doesn't need at its size — revisit only if this repo grows a third package.

## Before building this

**Review `ProWorks-Umbraco.AI-Page-Evaluator` directly** (see [research.md](research.md)) — an existing open-source project already doing AI-judged content review on top of Umbraco.AI (editors pick a doc type + AI profile + write/generate a prompt; scores clarity/SEO/brand-voice). It is scoped to page-level evaluation via prompt customization, not a general pluggable check-type framework, so it doesn't overlap with this project's architecture — but it may already have solved UI/prompt problems worth reusing rather than re-deriving, and its existence means this space has at least one other active author to be aware of (see risk in the [Overview](overview.md)).

## Build checklist

- [ ] Real AI-judged check: rubric scored by an Umbraco.AI profile, returning structured output (score, verdict, per-target feedback).
- [ ] **Rubric authoring UX, settled: free-form text.** Resolves [open question 1](overview.md) — a plain multi-line text field on the rubric check's `TConfig`, no guided/weighted-criteria form. Faster to author, and the AI-assisted authoring idea below only makes sense once free text is already the input shape it's assisting with.
- [ ] **The judge profile is configured separately from any generating profile** (finding G1). Warn in the UI when they resolve to the same profile.
- [ ] Structured output via a simplified schema + server-side transform, reusing the `IAISimplifiedPropertyValueTransformer` lesson from the Prompt package (`{}` schema nodes break OpenAI strict mode).
- [ ] Map provider failure, rate-limit, timeout and missing-key onto `NotEvaluated` — never onto `Fail`.
- [ ] Agent tools: `run_content_checks` (returns the report), `list_content_check_policies`.
- [ ] **Loop discipline in the tool layer (finding G2), settled: the tool enforces a hard round cap itself.** Resolves [open question 2](overview.md) — not the agent's system prompt, not just a tool-description hint an agent could ignore. Report round count and track best attempt so far regardless; the cap is what actually stops the loop, the reporting is what lets the caller see why.
- [ ] **Ship default rubrics** (finding K): tone-of-voice, readability, SEO value. A blank rubric box produces garbage.
- [ ] Delete [Core Contract](core-contract-spec.md)'s throwaway proving check B once this ships a real one.

## Future idea, deliberately not v1: AI-assisted rubric/rule authoring

Raised as a possible replacement for [open question 6](overview.md)'s power-user escape hatch (a sandboxed expression/"Script Check" type), not an addition alongside it: instead of an advanced user writing a raw expression for something a no-code form can't express, they write a plain-English description of what they want checked, and an AI-assisted authoring flow turns that into either a rubric (trivial, since rubric config is already free text per the decision above — the description more or less *is* the rubric) or, more ambitiously, an assembled set of existing deterministic check configs (a genuinely new "compile an English policy description into check rules" capability, not just filling in one field). **Keep as a future option — do not build for v1.** Recorded here rather than in the escape-hatch open question itself, because it's a different kind of thing: the Script Check idea is a new *check-authoring capability* (a check type that runs arbitrary logic); this is a new *authoring-assist* feature on top of check types that already exist, and conflating the two would blur why the escape hatch is deferred versus why this is. If this ever gets built, it doesn't change [Core Contract](core-contract-spec.md) at all — an AI that writes a `TConfig` for an existing check type is an authoring-time tool, not a new runtime contract shape.
