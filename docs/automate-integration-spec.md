# Automate Integration — `Umbraco.Community.ContentChecks.Automate`

**Status:** Design sketch, not yet a numbered build phase. New (2026-08-06).
**Part of:** [Overview](overview.md). Consumes [Core Contract](core-contract-spec.md)'s runner and results, and [Policy Resolution](policy-resolution-spec.md)'s resolution service; adds one small, generally-useful notification to Core along the way (§4).

---

## Why

[Triggers](triggers-spec.md) deliberately declined to build blocking support for scheduled publishing, bulk publish, or API-driven publishing — "there's no human to read the feedback." That stance still holds; this doesn't reopen it. What it doesn't give a site owner is any way to attach *deliberate*, *configurable* behavior to those moments — notify someone, escalate, branch on the result. Umbraco.Automate is Umbraco's own workflow/automation product, already used for exactly this kind of "when X happens, do Y" wiring. Integrating with it means ContentChecks doesn't have to invent bespoke automation logic for every scenario it already declined to handle directly — the site owner gets that control through a product built for it.

## Packaging

A new package, `Umbraco.Community.ContentChecks.Automate`, living in this repo — same reasoning as `Umbraco.Community.ContentChecks.AI`: ContentChecks owns the integration, not the other product. It depends on `Core` and on `Umbraco.Automate` as an ordinary NuGet reference.

**This generalizes an existing constraint rather than adding a new one.** [Overview](overview.md)'s global constraints already say Core takes no AI dependency, ever, and that the `.AI` package is where that dependency lives instead. The same rule now reads more generally: Core takes no dependency on any specific *other* Umbraco product — each integration is its own small bridge package. This is directly precedented already, not invented here: `Umbraco.AI.Automate` exists in the sibling Umbraco.AI monorepo for exactly this purpose, bridging Umbraco.AI to Umbraco.Automate without Umbraco.AI's own Core or Agent packages depending on it.

**Before building this, read `Umbraco.AI.Automate`'s actual source directly** — same methodology as everything else in this project (the check contract was built by reading Guardrails and EditableModels directly, not guessing at their shape). Two things specifically need verifying against real code, not assumed here: the actual shape of Automate's Action/Trigger extension API, and whether Automate has its own config-schema convention that this bridge package needs to conform to, or whether [Core Contract](core-contract-spec.md)'s `[ContentCheckField]` schema system can be reused as-is for rendering an Action's or Trigger's configuration inside Automate's own workflow-builder UI.

## Two directions — both wanted, not symmetric in complexity

### Action: a workflow step that calls into ContentChecks

A workflow step that runs checks against some content and exposes the result to later steps in the same workflow — this is what actually delivers "greater control than on-demand/on-save/on-publish": a scheduled workflow can pull a whole branch, run checks against each item, and branch on the outcome (email a report, flag pages, whatever the workflow author wires up), without ContentChecks knowing anything about schedules or bulk operations.

**Open: explicit or resolved check selection.** The Action's own config could name one check policy explicitly, or it could resolve whichever policies are already effective for the content being checked (reusing [Policy Resolution](policy-resolution-spec.md)'s Effective Checks computation, the same resolution any other trigger point would get). Leaning toward supporting both — explicit as an override, resolved as the default — but not decided.

**Needs a report summary a workflow's branching logic can actually use.** A full `ContentCheckReport` is a list of entries; a workflow condition step needs something simpler to compare against — an overall status, or at minimum a "does this report contain a blocking failure" flag. See §4 — this isn't really Automate-specific, so it's proposed as a small addition to [Core Contract](core-contract-spec.md) rather than something built only inside this package.

### Trigger: ContentChecks fires into Automate

More novel, and deliberately narrower in v1 scope than it could be.

**v1 scope: fire when a check run *produces* a matching result, not when a verdict *changes*.** "Notify me whenever any page fails a Blocking check" only needs to look at one run's output. "Notify me only when a page's verdict flips from passing to failing" needs comparing against a previous result — real, valuable, but it needs history the current design doesn't build for yet ([Core Contract](core-contract-spec.md)'s result cache stores the *last* verdict for cache-invalidation purposes, not a history a trigger could diff against). Deferred; recorded as a real enhancement, not dismissed.

**Mechanism: a plain, product-agnostic notification, not Automate-specific plumbing in Core.** Whatever completes a check run in [Core Contract](core-contract-spec.md) publishes a standard Umbraco notification — something like `ContentCheckReportCompletedNotification` — carrying the report plus enough context to filter on (content type alias, policy alias, check alias, mode, resulting status). This notification has no idea Automate exists; it's a general extensibility seam useful to anyone who wants to react to a completed run (logging, auditing, analytics — Automate is just its first real subscriber). This is what keeps the "no dependency on any specific other product" rule intact: Core publishes something generic, and the Automate bridge package is the one that subscribes to it and, when a run's result matches a workflow's configured trigger filter (built with Automate's own trigger-config UI — not something this package builds), starts that workflow.

## Small, prompted addition to Core Contract

An "overall status" for a whole `ContentCheckReport` — something like worst-status-wins across its entries, or an explicit `HasBlockingFailure` flag — doesn't exist yet. It's needed here for the Action's branching output, but it's not really Automate-specific: [Management API & UI](management-api-ui-spec.md)'s results panel currently only specs a per-check status badge, nothing for the report as a whole. Proposed as a small addition to [Core Contract](core-contract-spec.md) rather than siloed inside this package, since more than one consumer wants it. Not yet speced in detail — exact shape (a computed property vs. a separate summary type) is an open item there.

## Open questions

1. Explicit vs. resolved check selection in the Action (above).
2. Exact fields on the completion notification — depends on reading Automate's real extension API first, not decided from first principles here.
3. Verdict-*change* triggers (not just verdict-*produced*) — deferred; would need a result history [Core Contract](core-contract-spec.md) doesn't currently keep.
4. Whether Automate has its own config-schema convention this package must conform to, or whether [Core Contract](core-contract-spec.md)'s schema system is directly reusable for Action/Trigger configuration.
5. Exact shape of the proposed report-level "overall status" addition to [Core Contract](core-contract-spec.md).
