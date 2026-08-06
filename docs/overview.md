# Umbraco.Community.ContentChecks — Overview

**Status:** Draft, pre-build. **This is the `v18/dev` branch** (the default branch) — targets Umbraco CMS v18 only; v17 is maintained independently on the sibling `v17/dev` branch, see §1's CMS floor & branch model row. Phase 0 is complete on this branch. Phase 1 is built (two gaps open, see [Core Contract](core-contract-spec.md) §7). Phase 2 is now also built (two gaps open, see [Policy Resolution](policy-resolution-spec.md)'s Status line — no real-`IContent` test, and folder delete-with-children is unconfirmed). Phases 3+ still need re-planning now that Phase 2 is real too, not just designed.
**Owner:** Matt Brailsford

This is the mainline document — vision, naming, cross-cutting decisions, and the roadmap linking out to a spec per feature area. It doesn't duplicate the detail in those specs; it's the map, not the territory.

**Specs:**
- [Core Contract](core-contract-spec.md) — the check contract itself: `IContentCheck`, config schema, runner, caching (Phase 1)
- [Policy Resolution](policy-resolution-spec.md) — policies, element-type cascading, composition, Effective Checks (Phase 2)
- [Management API & Backoffice UI](management-api-ui-spec.md) — the CRUD/authoring surface and the editor-facing UI, including backtesting (Phase 3, Phase 6)
- [Triggers](triggers-spec.md) — on-demand, on-save, on-publish gating (Phase 4)
- [AI Integration](ai-integration-spec.md) — `Umbraco.Community.ContentChecks.AI`, the AI-judged check tier (Phase 5)
- [Automate Integration](automate-integration-spec.md) — `Umbraco.Community.ContentChecks.Automate`, two-way integration with Umbraco's workflow product (not yet a numbered phase)
- [Deploy Integration](deploy-integration-spec.md) — `Umbraco.Community.ContentChecks.Deploy`, moving check policies (and now folders) between environments (not yet a numbered phase; §1's disk-vs-volatile decision is a recommendation pending confirmation)
- [Notifications](notifications-spec.md) — every notification fired across the project: policy/folder CRUD (consumed by Deploy), and the two-tier run-completion pair (consumed by Automate)
- [Markup-Based Checking (Exploration)](markup-checks-exploration.md) — a distinct, still-open idea: checks against rendered output, not just properties
- [Market & Ecosystem Research](research.md) — what already exists, and what it validated or ruled out

---

## Goal

Ship a standalone Umbraco package that turns "what good content looks like" into executable, triggerable checks returning a status plus actionable feedback — so an AI agent can validate and iterate on content it generates, and human editors get a repeatable quality check.

## Source spec & sibling work

**Source spec:** Umbraco.AI `docs/superpowers/specs/2026-07-02-ai-native-content-editing-design.md` §3 (branch `v18/dev`, a different repo). That section is the functional vision this plan grew out of and supersedes its original phasing (see finding G4 below).

**Sibling work:** `github.com/mattbrailsford/Umbraco.Cms.DraftSets` (POC built, paused 2026-07-07). ContentChecks are the "self-validation" half of the same thesis; Draft Sets are the "isolation" half. ContentChecks do **not** block on Draft Sets, but the flagship AI-generation loop needs both (see Phase 7, and the Draft Sets discussion in the [markup-checks exploration doc](markup-checks-exploration.md)).

**Market validation:** confirmed as a real gap by direct research (see [research.md](research.md)) — every existing content-quality tool in the Umbraco ecosystem is either fixed-rule (vendor-coded, not editor-configurable) or, at best, lets editors toggle/tune existing checks (Preflight). Nobody offers a framework where developers register new check *types* in code AND editors compose their own check *policies* through a UI, spanning both deterministic and AI-judged checks. That combination is this project's actual product. Preflight, the closest analog, is also dead — no release since Feb 2024, none for Umbraco 14+, so it isn't even a usable option on v17/v18 today.

---

## 1. Naming & packaging (decided)

| Decision | Value | Rationale |
|---|---|---|
| Feature name | **Content Checks** | Was "Content Guards." Renamed 2026-08-05 after two collision risks surfaced: (1) Umbraco.AI **already ships a feature called Guardrails** (`AIGuardrailService`, `AIGuardrailRule`) for a different job — blocking/redacting unsafe AI chat output — so "ContentGuardrails" would misleadingly imply a connection to it; (2) three real community packages already use "\*Guard\*" for content-locking/permissions (`Cogworks.ContentGuard`, `Umbraco.Community.ContentLock`, `Umbraco-Community-PropertyGuard`), none doing quality checks, but crowding the same search terms. "Content Checks" avoids both and makes the positioning line below literal instead of a metaphor. |
| Package prefix | **`Umbraco.Community.*`** | Current recommended convention for community packages (default in the community package starter template). `Umbraco.*` is HQ-reserved on NuGet; `Our.Umbraco.*` is the older convention. |
| Core package id | `Umbraco.Community.ContentChecks` | Confirmed no existing package uses this name — see [research.md](research.md). |
| Root namespace | `Umbraco.Community.ContentChecks` | |
| Repo | `github.com/mattbrailsford/Umbraco.Community.ContentChecks` | Private first, public when solid. No donation path assumed, so no `Umbraco.Cms.*` namespace needed. |
| AI integration | `Umbraco.Community.ContentChecks.AI`, inside the **ContentChecks repo** (not the Umbraco.AI monorepo) | ContentChecks owns the check contract; the AI-judged tier is an implementation of that contract, not a feature of Umbraco.AI. Living beside Core lets it reuse the same ported config infra (see [Core Contract](core-contract-spec.md)) with a project reference, no cross-repo frontend dependency. It takes Umbraco.AI as a normal NuGet dependency (version range), the same direction any third-party consumer of Umbraco.AI would. Keeps Core dependency-free — the branch model below is adopted, but Umbraco.AI's heavier multi-product tooling (per-product changelog scopes, release-manifest include/exclude) still doesn't apply at this repo's size. |
| CMS floor & branch model | **v17 and v18, each maintained independently** — `v17/dev`/`v17/main` and `v18/dev`/`v18/main` (this branch), mirroring Umbraco.AI's own branch model exactly (checked directly, not assumed): `<major>/dev` for ongoing work, `<major>/main` tracking the latest release on that line, no forward-merge between lines. **This branch (`v18/dev`) pins `Umbraco.Cms.*` to `[18.0.0, 18.999.999)`** in `Directory.Packages.props` — the sibling `v17/dev` branch pins `[17.0.0, 17.999.999)`; the two are never widened to overlap. `version.json`'s `publicReleaseRefSpec` is updated to match `vN/`-prefixed branches (`^refs/heads/(v\d+/)?main$` etc.) — the same refspec-vs-branch-naming bug already hit and fixed once in the sibling Umbraco.AI repo, avoided here proactively rather than rediscovered later. **Superseded a prior decision recorded here** (a single wide package range spanning both majors on one branch) once the user asked for the real branch-per-major model instead — no new CMS core seams are needed for either version, so nothing about the check contract itself changes, only how the repo is organized to build and release against each. **Draft Sets integration (Phase 7) stays v18-only regardless** — a constraint of the separate Draft Sets repo/POC, unrelated to this branch model. |

**Positioning line to reuse:** *"Like Umbraco's Health Checks, but for content."* Now literal, not just an analogy. Health Checks already have the exact shape needed (a status plus a message, grouped, run on demand from a dashboard) and their result model solves finding G3 for free. Research ([research.md](research.md)) confirms this is a well-regarded, well-understood extension pattern in the Umbraco community, not just a convenient comparison.

**Sources for the naming/packaging decisions above:**
- [Umbraco.Community naming convention — opinionated-package-starter](https://github.com/LottePitcher/opinionated-package-starter)
- [NuGet package ID prefix reservation](https://learn.microsoft.com/en-us/nuget/nuget-org/id-prefix-reservation)
- [History of Our.Umbraco package names](https://dev.to/leekelleher/history-of-our-umbraco-package-names-34f2)

---

## 2. What this is not

- **Not a replacement for doc-type validation.** The real difference is not "above field level" — several headline examples (required main image, meta description length) *are* field-level. The difference is that checks are **contextual** (apply to one branch or one check policy, not the whole doc type globally) and **advisory-capable** (warn without blocking). Mandatory + regex on a doc type is schema-wide and always hard.
- **Not a workflow / approval / permission engine.** Blocking on publish is a hard edge here; see [Triggers](triggers-spec.md) for the deliberately narrow stance.
- **Not the AI iteration loop itself.** The package exposes "run the checks and give me a report." The *agent* iterates. This keeps the core dependency-free and keeps convergence control where the budget lives.

(This sharpens the source spec's §3.6, which was muddled on this point — finding J below.)

---

## 3. Interrogation findings (master reference)

These findings came from an adversarial interrogation of the source spec's Content Validators section, before any contract was drafted. They exist nowhere else, and every spec in this project references them by code (e.g. "finding G1") rather than restating them — this table is the canonical definition.

| # | Finding | Where handled |
|---|---|---|
| G1 | AI can talk itself into a pass (marks own homework) | [AI Integration](ai-integration-spec.md) — judge is a **separately configured** profile, distinct from the generating profile; report always exposes round count and raw judge output |
| G2 | Loop may never converge; unbounded token cost | [Core Contract](core-contract-spec.md) (cheap-first ordering + short-circuit), [AI Integration](ai-integration-spec.md) (round cap, best-attempt tracking) |
| G3 | Pass/fail is the wrong result shape | [Core Contract](core-contract-spec.md) — 5-state status + optional score/threshold |
| G4 | Contract designed without its hardest consumer | [Core Contract](core-contract-spec.md) — a real AI-judged check is built **in the same phase** as the contract, not phased after it |
| E | Non-interactive publish paths (scheduled, bulk, descendants, Deploy) | [Triggers](triggers-spec.md) — explicit per-trigger opt-in; never block non-interactive publishes in v1 |
| F | Single-item scope is a one-way door | [Core Contract](core-contract-spec.md) — no `Scope` enum needed; only item-level exists for v1, and Branch/Set arrive later as a new opt-in interface, not a retrofit — enum/interface additions are non-breaking, so nothing is actually foreclosed |
| G | Variants unaddressed | [Core Contract](core-contract-spec.md) — culture + segment on context and on every result target |
| H | Verdict flicker; no caching or versioning | [Core Contract](core-contract-spec.md) — cache keyed on the content item's own version id (`int? VersionId`) + check alias + check-assembly fingerprint (automatic, no manual version) + resolved-config hash; skipped entirely for unsaved/Draft Set content (no `VersionId`, and no generic way to hash an opaque snapshot's values instead) |
| I | Feedback has no address to point at | [Core Contract](core-contract-spec.md) — `ContentCheckTarget` on every result |
| J | Non-goal boundary muddled | §2 above |
| K | Rubric authoring untested, no shipped defaults | [AI Integration](ai-integration-spec.md) (default rubrics), [Management API & UI](management-api-ui-spec.md) (backtest) |
| L | Checks must read pending Draft Set, not trunk | [Core Contract](core-contract-spec.md) — context carries a **resolved snapshot**, never an id to look up |
| M | Scoping unit undefined ("section" means backoffice section in Umbraco) | [Policy Resolution](policy-resolution-spec.md) — each check policy is 1:1 to a content type (document type or element type), resolved directly, via composition, or via cascading, plus an optional start node; the word "section" is banned in this codebase |
| N | Unified contract hides cost and determinism from callers | [Core Contract](core-contract-spec.md) — `ContentCheckCost` and `ContentCheckKind`, two independent properties directly on `IContentCheck` |

---

## 4. Repo layout

Mirrors `Umbraco.Cms.DraftSets`, which mirrors `Umbraco.Cms.Search`.

```
Umbraco.Community.ContentChecks/
├── src/
│   ├── Umbraco.Community.ContentChecks.Core/          # contract, runner, built-in checks, cache, ported config infra
│   ├── Umbraco.Community.ContentChecks.Web/           # Management API controllers
│   ├── Umbraco.Community.ContentChecks.Web.StaticAssets/Client/   # Lit + TS backoffice UI
│   ├── Umbraco.Community.ContentChecks.Persistence*/  # Phase 2, built — EF Core + SQL Server + SQLite
│   ├── Umbraco.Community.ContentChecks.AI/            # Phase 5 — AI-judged check, depends on Umbraco.AI (NuGet)
│   ├── Umbraco.Community.ContentChecks.Automate/      # bridge to Umbraco.Automate, see Automate Integration spec
│   ├── Umbraco.Community.ContentChecks.Deploy/        # bridge to Umbraco Deploy, see Deploy Integration spec
│   └── Umbraco.Community.ContentChecks/               # meta-package
├── tests/  (Tests.Unit, Tests.Integration)
├── scripts/install-demo-site.{sh,ps1}                 # demo site gitignored, generated per-dev
├── Umbraco.Community.ContentChecks.slnx
└── version.json                                       # NBGV — single version for the whole repo; see AI Integration spec's packaging note
```

---

## 5. Phase roadmap

| Phase | Spec | Status |
|---|---|---|
| 0 — Bootstrap | (below) | **Complete on this branch** |
| 1 — Contract, runner, two proving checks | [Core Contract](core-contract-spec.md) | Built and passing its own tests; two gaps open (see that spec's §7) |
| 2 — Check policies & configuration | [Policy Resolution](policy-resolution-spec.md) | Built and unit-tested; real-`IContent` integration test still open (see that spec's Status line) |
| 3 — Management API & backoffice UI | [Management API & UI](management-api-ui-spec.md) | Sketch only |
| 4 — Triggers | [Triggers](triggers-spec.md) | Sketch only |
| 5 — `Umbraco.Community.ContentChecks.AI` | [AI Integration](ai-integration-spec.md) | Sketch only |
| 6 — Backtest | [Management API & UI](management-api-ui-spec.md) | Sketch only |
| 7 — Draft Sets integration | (below) | Not started |
| Unnumbered — Automate integration | [Automate Integration](automate-integration-spec.md) | Design sketch; not yet placed in the sequence above |
| Unnumbered — Deploy integration | [Deploy Integration](deploy-integration-spec.md) | Design sketch; disk-vs-volatile decision recommended, not yet confirmed |

### Phase 0 — Bootstrap (complete on `v18/dev`)

- [x] Create repo `mattbrailsford/Umbraco.Community.ContentChecks`, private. (User created it directly; local scaffold pushed 2026-08-05.)
- [x] Copy the structural scaffolding from `Umbraco.Cms.DraftSets`: `.slnx`, `version.json` + NBGV, `Directory.Packages.props`, `.editorconfig`, `.gitignore`, `scripts/install-demo-site.{sh,ps1}`, test project layout. Version started at `0.1.0-alpha` (new product, not tracking a CMS major the way Draft Sets does) — flagged as an assumption, revisit if wrong.
- [x] Set CMS floor `[18.0.0, 18.999.999)`, TFM `net10.0` — tightened from the original `[18.0.0, 19)` once `v17/dev` split off, per §1's branch model.
- [x] Generate the demo site, confirm it boots. Confirmed: `dotnet run` reaches "Application started" on `https://localhost:44377` with no exceptions.
- ~~Project-reference the package into the Umbraco.AI demo site for fast iteration~~ — **dropped, superseded by a global rule:** no project reference ever crosses between this repo and the Umbraco.AI monorepo, in either direction. This repo's own demo site is the one place to develop and verify it. If the `.AI` project (Phase 5) ever needs to exercise real Umbraco.AI behavior during development, pull Umbraco.AI's own published NuGet package (its nightly/prerelease feed) into *this* repo's demo site — never a cross-repo project reference. Keeps the two repos genuinely decoupled, not just decoupled in the shipped package.

**Acceptance:** empty package installs into a running Umbraco 18 site with no errors. **Met**, via ContentChecks' own demo site. (v17's equivalent acceptance is tracked on the `v17/dev` branch, independently.)

### Phase 7 — Draft Sets integration (not started)

- [ ] `IContentCheckSnapshot` adapter over a Draft Set's pending state, so checks validate the set rather than trunk (finding L).
- [ ] Lives in the Draft Sets repo or a small bridge package, not in ContentChecks core.
- [ ] Unblocks the full flagship loop: open a set → generate → run checks → iterate → one approval at apply time.

See also the Draft Sets discussion in the [markup-checks exploration doc](markup-checks-exploration.md) — that thread distinguishes the *simple* case (a normal saved draft, servable by Umbraco's own native preview) from the *harder* case Draft Sets itself is solving (in-memory, not-yet-persisted edits), which is what this phase actually depends on.

---

## 6. Global constraints

- **Core package takes no dependency on any specific other Umbraco product.** Ever. `Umbraco.Community.ContentChecks.Core` references Umbraco CMS and nothing else. Each integration is its own bridge package that depends on Core *and* the product it integrates with — never the reverse, and never folded into Core itself. This started as "no AI dependency" (`.AI` depends on Core and Umbraco.AI) and generalizes the same way for `.Automate` (see [Automate Integration](automate-integration-spec.md)) and `.Deploy` (see [Deploy Integration](deploy-integration-spec.md)) — Umbraco.AI, Umbraco.Automate, and Umbraco Deploy are each a dependency of exactly one bridge package, never of Core or of each other.
- **Two ways in to Umbraco.AI specifically, not one.** `Umbraco.Community.ContentChecks.AI` calls the runner in-process (for sites that also run Umbraco.AI); the Umbraco MCP calls the Management API (for sites that don't).
- Async naming `[Action][Entity]Async`; repositories internal to their service; extension methods in `Umbraco.Community.ContentChecks.Extensions`.
- Conventional Commits, sentence-case subject.
- Never break a public API once published. The Phase 1 contract (see [Core Contract](core-contract-spec.md)) is the risk surface — hence its review gate.
- Prerelease versions use dotted identifiers (`-alpha.1`, `-beta.1`), never `-beta1`.

---

## 7. Open questions (master list)

1. ~~**Rubric authoring UX**~~ — **resolved: free-form text.** See [AI Integration](ai-integration-spec.md)'s build checklist. A plain multi-line field on the rubric check's `TConfig`, no guided/weighted-criteria form.
2. ~~**Who owns the round cap**~~ — **resolved: the tool itself, as a hard limit.** Not the agent's system prompt, not just a description hint. See [AI Integration](ai-integration-spec.md).
3. ~~**v17 support**~~ — **resolved: both v17 and v18, on independent branches.** See §1's CMS floor & branch model row — `v17/dev`/`v17/main` and `v18/dev`/`v18/main` (this branch), mirroring Umbraco.AI exactly, checked directly. Phase 0 on this branch (`v18/dev`) needed no rework beyond tightening the package range; `v17/dev` redid its own Phase 0 CMS-floor step independently.
4. **Scoring transparency** — deliberately not decided yet, on purpose: build the results UI first and judge from the real thing, not from imagining it in the abstract. Revisit once [Management API & UI](management-api-ui-spec.md)'s results panel exists.
5. **Does a check ever get to see the whole site?** Still genuinely undecided — a Branch/Set scope for a single check's `EvaluateAsync` is deferred past v1, and the contract must not preclude it. See [Core Contract](core-contract-spec.md). **Distinct, lower-priority idea recorded separately, not a resolution of this:** a bulk "run every applicable policy across a branch/site in one sweep" endpoint — an operational, many-items-at-once feature built on the existing one-snapshot-at-a-time check contract, not a new scope for checks themselves. See [Management API & UI](management-api-ui-spec.md)'s "Run-all endpoint."
6. **Power-user escape hatch** — Drupal's Field Validation module ships a "Callback"/"Expression" validator alongside its picklist of no-code validators, so an edge case that doesn't justify a full C# assembly can still be handled without one. Worth a "Script Check" type in v2 (a sandboxed expression evaluated against the snapshot), explicitly deferred — flagged here so the contract doesn't quietly foreclose it. See [research.md](research.md). **A possible alternative to this, also deferred:** AI-assisted authoring — an advanced user describes what they want in plain English instead of writing an expression, and it gets turned into rubric text or an assembled set of existing check configs. Recorded as its own future idea, not a variant of the Script Check one — see [AI Integration](ai-integration-spec.md).
7. **Cascading cost across element-type checks** — an element-type policy runs once per instance found anywhere in a document's block structure, so a `Metered` rule on a common component (Testimonial, Feature Card) multiplies by however many times that component appears on the page. Needs a deliberate stance in [Policy Resolution](policy-resolution-spec.md): batch the calls, cap the instance count per run, surface the multiplication to the editor before they trigger it, or accept it as the real cost of the feature. **Partially mitigated for repeat runs, not resolved for a first run:** [Core Contract](core-contract-spec.md) §5's version-keyed cache means a page that's already been evaluated skips every unchanged instance on the next run — real savings, but it does nothing for the first time twenty instances are evaluated in one sweep. The batch/cap decision for that first run is still open; must not be left to fall out of whatever the traversal implementation happens to do first.

~~Port vs. depend, for the config infra~~ — **resolved.** `Umbraco.Community.ContentChecks.AI` living in this repo means it project-references Core's ported infra directly. No cross-repo frontend dependency question remains.

**Not yet merged into this list:** [Automate Integration](automate-integration-spec.md) and [Deploy Integration](deploy-integration-spec.md) each carry their own open questions, not duplicated here to avoid the two copies drifting apart — check those specs directly rather than assuming this list is exhaustive.

---

## 8. Risks

- **The contract is a one-way door.** Published, it cannot be reshaped. The Phase 1 review gate ([Core Contract](core-contract-spec.md)) is the mitigation, and it is not optional.
- **Rubric quality is the actual product.** All the engineering here is scaffolding around whether an editor can write a rubric that produces stable, useful judgments. [AI Integration](ai-integration-spec.md) and the backtest capability in [Management API & UI](management-api-ui-spec.md) carry the real risk.
- **Checks shipped alone give you the editor tool, not the pitch.** The flagship AI story needs Draft Sets for isolation. Without it, "AI iterates until it passes" means writing revision after revision onto the shared live draft while the editor watches.
- **Scope creep into workflow.** Blocking-on-publish is one small step from an approval engine. Hold the [Triggers](triggers-spec.md) line.
- **A second, unaffiliated project is already building AI-judged content review** (`ProWorks-Umbraco.AI-Page-Evaluator` — see [research.md](research.md)). Not a blocker — different architecture, different scope — but worth tracking so [AI Integration](ai-integration-spec.md) doesn't quietly reinvent something it already solved, and so positioning is deliberate if the two projects are ever compared publicly.
