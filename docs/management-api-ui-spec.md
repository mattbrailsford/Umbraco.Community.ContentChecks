# Management API & Backoffice UI

**Status:** Build sketch only — needs a proper design pass once [Core Contract](core-contract-spec.md) is actually built (Phase 3, plus backtesting from Phase 6).
**Part of:** [Overview](overview.md). Exposes [Core Contract](core-contract-spec.md) and [Policy Resolution](policy-resolution-spec.md); doesn't redefine anything from either.

---

## Why this isn't optional polish

Check policies are editor-authored, so the UI is the product, not a wrapper around it. No content person is going to hand-edit a DB row to define a rubric.

## Management API (`Umbraco.Community.ContentChecks.Web`)

- [ ] Run checks for a content item (returns `ContentCheckReport` — see [Core Contract](core-contract-spec.md)).
- [ ] List/CRUD check policies and their rules (see [Policy Resolution](policy-resolution-spec.md)).
- [ ] List registered check *types* with their config schema (`GetConfigSchema()` — see [Core Contract](core-contract-spec.md)) — the UI renders every check's config form from this, no per-type frontend code.
- [ ] **Effective Checks endpoint** ([Policy Resolution](policy-resolution-spec.md)): given a content type alias, return every applicable policy with its provenance (Direct / Via composition / Via nested element) — schema-level, computable with no live content.
- [ ] OpenAPI client generation, matching the Umbraco.AI frontend toolchain (Lit + TS + Vite + @hey-api/openapi-ts).

## Backoffice UI (`Umbraco.Community.ContentChecks.Web.StaticAssets/Client`), Lit + TypeScript

- [ ] **Check policy editor** — a dedicated workspace: name/alias, a content-type picker (one type — doc type or element type, see [Policy Resolution](policy-resolution-spec.md)), an optional start-node picker, add rules from the registered-type list, per-rule config form (rendered by `uai-model-editor`-equivalent, ported per [Core Contract](core-contract-spec.md) — no bespoke form per check type), mode toggle (Advisory/Blocking), `NotEvaluated` fail-open/fail-closed toggle.
- [ ] **Effective Checks view** ([Policy Resolution](policy-resolution-spec.md)) — a table of every check that governs a content type, each row's provenance labeled plainly (Direct / Via composition `[X]` / Via nested element `[Y]` in property `[Z]`). This is the safety net for dynamic composition resolution and cascading, not a discoverability nicety — a `Blocking` check appearing because of an unrelated composition or block-config edit needs to be *visible* before it surprises someone at publish time. Stretch goal: surface it directly on Umbraco's own Doc Type/Element Type editor (bigger integration, extends a core workspace rather than this package's own section); fallback: host the same computed view inside Content Checks' own section if that proves too invasive.
- [ ] **Results panel**, embedded in the content app (property-editor-adjacent, not a separate section): per-check status badge, message (which is where any suggested fix is expressed, in prose — see [Core Contract](core-contract-spec.md)), score-vs-threshold bar when present, click-through from a `ContentCheckTarget` straight to the offending property/element/variant.
- [ ] **Run button** — on-demand trigger from the content app, shows a live/polling report while metered checks are still running (these can be slow; don't block the UI thread on an LLM call).
- [ ] **Health-check-style dashboard** — run a check policy across a branch/tree, see the status distribution. Doubles as the backtest surface below, so build the report-across-many-items view once and reuse it.
- [ ] Component export convention: everything through `index.ts`/`export.ts`, never import by path (per repo CLAUDE.md lesson).

**Acceptance:** an editor with no dev involvement can create a check policy, attach a required-image check and a stub rubric check to a doc type, run it against a real page from the content app, and see a report with a click-through to the missing property — **and** can open the Effective Checks view for a type governed by a composition-wide policy, a type-specific policy, and a cascaded element-type policy simultaneously, and correctly identify which is which from the provenance labels alone.

---

## Backtest (Phase 6)

The thing that makes rubrics trustworthy (finding K in the [Overview](overview.md)). Small, high-value, easy to skip and shouldn't be.

- [ ] Run a check policy across an existing content branch and show the status distribution.
- [ ] Surface it in the check policy editor as "test this against your real content."
- [ ] Rationale: if a new rubric fails 90% of the live site, the rubric is wrong, not the site. Without this, checks get authored blind and abandoned.

This directly reuses the health-check-style dashboard above rather than being a separate build — same report-across-many-items view, different entry point.
