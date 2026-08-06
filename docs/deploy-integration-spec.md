# Umbraco Deploy Integration — `Umbraco.Community.ContentChecks.Deploy`

**Status:** Design sketch, not yet a numbered build phase. New (2026-08-06). §1's decision is a **recommendation pending confirmation**, not settled — everything downstream of it assumes the recommendation stands, but it hasn't been signed off yet.
**Part of:** [Overview](overview.md). Moves [Policy Resolution](policy-resolution-spec.md)'s `ContentCheckPolicy` between environments; defines nothing new about the policy model itself.

---

## 1. The decision: disk-based artifacts, not a volatile transfer queue

**Recommended: `.uda`-style disk artifacts, checked into source control, following the exact precedent Umbraco.AI's own Guardrails feature already set.**

Umbraco.AI faced this identical question for `AIGuardrail` — a DB-persisted, developer/architect-authored, environment-portable configuration entity, structurally the same shape as `ContentCheckPolicy` (a named thing, an attachment target, a list of child rule records) — and it already ships a real answer: `AIGuardrailArtifact` (the serializable representation), `UmbracoAIGuardrailServiceConnector` (converts between the live entity and its artifact, handles applying an artifact to an environment), and a pair of Deploy Refresher notification handlers (`AIGuardrailSavedDeployRefresherNotificationAsyncHandler`, `AIGuardrailDeletedDeployRefresherNotificationAsyncHandler`) that write, update, or remove the on-disk artifact whenever the entity itself is saved or deleted in the backoffice. **Before building this, read that code directly** — same methodology as everywhere else in this project (the check contract was built by reading Guardrails and EditableModels directly, not guessing at their shape).

**Why disk-based fits `ContentCheckPolicy` specifically, not just "because Guardrails did it":**

- A check policy is schema-level configuration, not end-user content — "Blog Post gets a Blocking SEO check" is the same *kind* of decision as "Blog Post gets a mandatory `metaDescription` property," and mandatory-property decisions already move between environments as disk artifacts (Document Type `.uda` files), reviewed in a PR, deployed by CI. Content items move a completely different way (Deploy's UI-driven transfer queue), because content is created per-environment and moved deliberately, one at a time or in batches, not synced continuously alongside code.
- **Git-diffability matters here specifically, for a reason this project has already emphasized once before.** [Policy Resolution](policy-resolution-spec.md) built Effective Checks *because* dynamic composition-matching and cascading mean a policy's reach can change from an edit that has nothing to do with the policy itself — and the whole point of that feature is making an invisible change visible before it surprises someone. A volatile, UI-transfer-only policy could change silently between environments with no diff anyone reviewed before it shipped to production — undermining the same principle Effective Checks exists to protect, just at the deployment layer instead of the authoring layer. A `.uda` file in a pull request is a check policy's own effective-checks-style visibility, extended to "what's about to change when this deploys."

**This was originally drafted assuming `ContentCheckPolicy` stored the content type's *alias* — that was wrong, and [Policy Resolution](policy-resolution-spec.md) now stores its Key instead. The correction turns out to help Deploy specifically, not just rename-safety within one environment:**

- An alias reference would have had *no real Deploy dependency machinery behind it at all* — it's just a string, so a `.uda` artifact carrying an old alias would either fail to find a match after a rename, or silently match whatever unrelated type happens to hold that alias now. Deploy's own dependency-resolution system is Key/UDI-based throughout; a bare alias string sits outside it entirely.
- `ContentTypeKey`, by contrast, is exactly the kind of reference Deploy already knows how to carry correctly across environments and through a rename — **as long as the Connector declares it as a real dependency** (§2), the same treatment `StartNodeKey` below needs. Both fields end up needing the same kind of dependency declaration, which is a simpler, more consistent story for the Connector to implement than having one field need it and the other not.
- The one identifier that *is* safely a plain alias, correctly: each rule's check reference (compile-time, from the check's `[ContentCheck(...)]` attribute — see [Core Contract](core-contract-spec.md)). Check *types* are code-registered, not renamable backoffice entities, so there's no equivalent rename risk to design around.

**`StartNodeKey` needs the same kind of dependency declaration as `ContentTypeKey`.** A start node is a specific content item, referenced by its own Key — and content item identity is exactly the thing Deploy's dependency-resolution system exists to handle (resolving "this same logical item" across environments via UDI, and — this is the real question a Connector has to answer, not sidestep — deciding what happens if that item hasn't been transferred to the target environment yet). The Connector needs to declare this as a real dependency, the same way a Document Type artifact declares a dependency on the Data Types its properties use, not treat it as an incidental field.

**The alternative considered, and why it's the worse fit:** a volatile model, where policies live only in the DB and move between environments through Deploy's UI-driven transfer queue (the same mechanism used for moving individual content items). This is the right mechanism for occasional, deliberate, one-at-a-time transfers — not for configuration that should stay continuously in sync across every environment as part of normal CI/CD, which is what schema-like settings (and, by the same reasoning, check policies) actually need. Using the content-transfer mechanism for a schema-shaped problem is solving it with the wrong tool, not just a different one.

## 2. What needs building

- `ContentCheckPolicyArtifact` — the serializable representation. Mirrors `AIGuardrailArtifact`'s shape: whatever identifies the policy, plus its `ContentTypeKey`, `StartNodeKey` (if set), and its rules (check alias, config JSON, mode, `NotEvaluated` policy). Both Key fields are dependencies the Connector must declare (§1), not incidental data on the artifact.
- A Service Connector (mirrors `UmbracoAIGuardrailServiceConnector`) — `GetArtifact`/`Process` (or whatever the actual current Deploy connector interface calls them; verify against real source, don't assume the method names from memory of the pattern alone). Declares dependencies on:
  - the referenced content type's own Deploy artifact (Document Type/Element Type artifacts already exist in Umbraco Core's own Deploy support — this is a dependency *declaration*, not new machinery to build);
  - the start node's content item, if set, via its UDI.
- Deploy Refresher notification handlers on `ContentCheckPolicy` save/delete (mirrors the two Guardrail handler classes exactly) — write, update, or remove the on-disk artifact whenever a policy changes in the backoffice.

**Open, not asserted:** the exact on-disk file format and location convention (naming, folder) for the artifact. This has almost certainly moved across Umbraco Deploy versions — needs checking against current Umbraco.Deploy documentation or source when this is actually built, not asserted from memory of the general pattern.

## 3. Packaging

**A separate bridge package, `Umbraco.Community.ContentChecks.Deploy`, in this repo** — same reasoning as `.AI` and `.Automate` ([AI Integration](ai-integration-spec.md), [Automate Integration](automate-integration-spec.md)): Core takes no dependency on any specific other product, and Umbraco Deploy, while first-party, is still something not every Umbraco site runs. Depends on Core plus whichever `Umbraco.Deploy.*` NuGet packages the connector interfaces actually live in (verify the exact package names against current Umbraco.Deploy — don't assume from the general Umbraco.AI.Deploy pattern). This also matches Umbraco.AI's own product family structure directly: `Umbraco.AI.Deploy`, `Umbraco.AI.Prompt.Deploy`, and `Umbraco.AI.Agent.Deploy` are each their own package, never folded into Core or the feature package they support.

## 4. Scope

**Only `ContentCheckPolicy` (and its rules) needs Deploy support.** Check *types* (`IContentCheck` implementations) are code, not database entities — they deploy the ordinary way, through the NuGet package or project reference that ships them, same as check evaluator types in Umbraco.AI never needed an artifact of their own; only the DB-persisted `AIGuardrail`/`AIProfile`/`AIConnection` entities did.

## 5. Open questions

1. **§1's recommendation itself** — disk-based artifacts, pending confirmation.
2. Exact on-disk artifact format/location — verify against current Umbraco.Deploy source/docs.
3. Exact `Umbraco.Deploy.*` package names this bridge package should depend on — verify, don't assume from the Umbraco.AI.Deploy pattern alone.
4. Whether a future check type's config JSON could ever need its own Deploy-aware dependency resolution (e.g. if a config field held a raw content or media reference instead of an alias). Not needed by any check type currently speced — flagged as an extensibility question for the Connector, not a v1 requirement.
