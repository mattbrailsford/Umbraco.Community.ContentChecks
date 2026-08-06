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
- [ ] **Tree + folder endpoints** (§"Tree and folders" below): children-of-folder/root, ancestors (breadcrumb), folder create/rename-or-move/delete. Moving a *policy* into or out of a folder is not a separate endpoint — it's the `ParentFolderKey` field on the existing policy update call ([Policy Resolution](policy-resolution-spec.md) §4).
- [ ] OpenAPI client generation, matching the Umbraco.AI frontend toolchain (Lit + TS + Vite + @hey-api/openapi-ts).

## Tree and folders

Prompted directly: policies should be organized in a tree, with folders an editor creates, the way Data Types/Document Types/Media Types already work — not the flat list Umbraco.AI's own Profiles/Connections/Guardrails use today (checked directly: none of them have a tree or a folder concept, so they weren't a usable precedent here).

**Real precedent, checked directly, once Umbraco.AI came up empty: Umbraco.Automate's own automation tree.** It solves the identical problem — a custom, EF-backed entity needing folder organization in its own backoffice tree — without using Umbraco CMS Core's native tree/folder base classes (`EntityTreeControllerBase`, `FolderTreeControllerBase<T>`) at all. Its shape, adopted here:

- **Backend:** plain, hand-rolled REST controllers (not the native CMS tree/folder base classes) exposing children-of-root, children-of-folder, ancestors (for breadcrumb), and folder create/update(rename-or-move)/delete. `ContentCheckPolicyFolder` is the data model ([Policy Resolution](policy-resolution-spec.md) §4); moving a *policy* into or out of a folder is just its `ParentFolderKey` field on the normal policy update call, not a dedicated endpoint — same as Automate does for its own automations.
- **Frontend:** rides Umbraco's native tree *contracts* (`UmbTreeDataSource`, `UmbDefaultTreeElement`/`UmbDefaultTreeContext`) against this package's own data source implementation, exactly as Automate's `UaAutomationTreeServerDataSource` does — proving a third-party EF entity can use the native tree UI plumbing without needing the native tree UI's backend base classes to match it.
- **Content Checks' own section root becomes a `kind: "tree"` menu item**, replacing what would otherwise be a flat `entityContainer`/`collection` view (the pattern Umbraco.AI uses, now known not to fit this need).

**Delete-with-children is an open call, not yet confirmed** — see [Policy Resolution](policy-resolution-spec.md) §4: Automate cascades (deleting a folder deletes everything inside it), but the recommendation here is to block delete on a non-empty folder instead, since a check policy governs real publish-blocking behavior and losing several of those silently is a bigger footgun than losing some automations.

## Backoffice UI (`Umbraco.Community.ContentChecks.Web.StaticAssets/Client`), Lit + TypeScript

- [ ] **Policy tree** — replaces a flat list as the section's root view (above): folders and policies as sibling nodes, create/rename/move/delete folder from the tree's own context menu, drag-and-drop (or a move dialog, whichever the native tree contracts make cheaper) to re-parent a policy or folder.
- [ ] **Check policy editor** — a dedicated workspace: name/alias, a content-type picker (one type — doc type or element type, see [Policy Resolution](policy-resolution-spec.md)), an optional start-node picker, add rules from the registered-type list, per-rule config form (rendered by `uai-model-editor`-equivalent, ported per [Core Contract](core-contract-spec.md) — no bespoke form per check type), mode toggle (Advisory/Blocking), `NotEvaluated` fail-open/fail-closed toggle.
- [ ] **Effective Checks view** ([Policy Resolution](policy-resolution-spec.md)) — a table of every check that governs a content type, each row's provenance labeled plainly (Direct / Via composition `[X]` / Via nested element `[Y]` in property `[Z]`). This is the safety net for dynamic composition resolution and cascading, not a discoverability nicety — a `Blocking` check appearing because of an unrelated composition or block-config edit needs to be *visible* before it surprises someone at publish time. Stretch goal: surface it directly on Umbraco's own Doc Type/Element Type editor (bigger integration, extends a core workspace rather than this package's own section); fallback: host the same computed view inside Content Checks' own section if that proves too invasive.
- [ ] **Results panel**, embedded in the content app (property-editor-adjacent, not a separate section): per-check status badge, message (which is where any suggested fix is expressed, in prose — see [Core Contract](core-contract-spec.md)), score-vs-threshold bar when present, click-through from a `ContentCheckTarget` straight to the offending property/element/variant.
- [ ] **Run button** — on-demand trigger from the content app, shows a live/polling report while metered checks are still running (these can be slow; don't block the UI thread on an LLM call).
- [ ] **Health-check-style dashboard** — run a check policy across a branch/tree, see the status distribution. Doubles as the backtest surface below, so build the report-across-many-items view once and reuse it.
- [ ] Component export convention: everything through `index.ts`/`export.ts`, never import by path (per repo CLAUDE.md lesson).

**Acceptance:** an editor with no dev involvement can create a check policy, attach a required-image check and a stub rubric check to a doc type, run it against a real page from the content app, and see a report with a click-through to the missing property — **and** can open the Effective Checks view for a type governed by a composition-wide policy, a type-specific policy, and a cascaded element-type policy simultaneously, and correctly identify which is which from the provenance labels alone — **and** can create a folder, move an existing policy into it, and see the tree reflect both without a page reload.

---

## Backtest (Phase 6)

The thing that makes rubrics trustworthy (finding K in the [Overview](overview.md)). Small, high-value, easy to skip and shouldn't be.

- [ ] Run a check policy across an existing content branch and show the status distribution.
- [ ] Surface it in the check policy editor as "test this against your real content."
- [ ] Rationale: if a new rubric fails 90% of the live site, the rubric is wrong, not the site. Without this, checks get authored blind and abandoned.

This directly reuses the health-check-style dashboard above rather than being a separate build — same report-across-many-items view, different entry point.

## Run-all endpoint (low priority)

Raised alongside [open question 5](overview.md): not "should one check be able to see the whole site" (that's [Core Contract](core-contract-spec.md)'s `Scope` cut, still genuinely undecided as a check-authoring question — a single check's `EvaluateAsync` still only ever sees one snapshot), but a much simpler, purely operational ask: an endpoint that runs *every* applicable policy across a branch or the whole site in one sweep, rather than one policy at a time the way Backtest above is scoped. Low priority, not v1-blocking — recorded so it isn't lost, not scheduled.

**This reuses the same report-across-many-items mechanism as Backtest**, just resolving policies per item (via [Policy Resolution](policy-resolution-spec.md)'s resolution service) instead of running one named policy against every item. **The version-keyed cache ([Core Contract](core-contract-spec.md) §5) is what makes this affordable to actually run on a schedule**, not just on demand: a repeat sweep only pays for items that changed since the last one, so "run all checks across the site" degrades gracefully from "expensive full audit" the first time to "cheap incremental check" every time after — without this endpoint needing any cache-awareness of its own, since that's the underlying `IContentCheckResultCache`'s job regardless of caller.
