# Notifications

**Status:** Design settled (2026-08-06). Not yet built — lands alongside whichever phase owns the firing entity ([Policy Resolution](policy-resolution-spec.md) Phase 2 for the CRUD ones, [Core Contract](core-contract-spec.md) Phase 1 / Policy Resolution Phase 2 for the run-completion ones).
**Part of:** [Overview](overview.md). Cuts across [Core Contract](core-contract-spec.md) (runner fires one), [Policy Resolution](policy-resolution-spec.md) (`ContentCheckPolicy` and its new folder entity fire the CRUD set; the resolution layer fires the other), [Automate Integration](automate-integration-spec.md) (Trigger direction subscribes), and [Deploy Integration](deploy-integration-spec.md) (Refresher handlers subscribe).

---

## Why this is its own doc

Every other spec in this project either fires a notification or consumes one, but none of them owns the concept — writing it into whichever spec got there first would leave the other three cross-linking into a corner of a doc about something else. This doc is the canonical definition; the specs above reference it rather than restate it, same convention as the [Overview](overview.md)'s findings table.

## Researched before writing this — real precedent, not guessed

Two directly relevant precedents in the sibling Umbraco.AI and Umbraco.Automate monorepos, read directly rather than assumed:

- **CRUD notification shape:** Umbraco.AI's own `AIGuardrail` doesn't use Umbraco CMS's native `SavingNotification<T>`/`SavedNotification<T>` — those carry a **collection** (`SavedEntities`), built for entities the CMS saves in batches. Umbraco.AI defines its own single-entity base classes instead (`AIEntitySavingNotification<T>`, `AIEntitySavedNotification<T>`, `AIEntityDeletingNotification<T>`, `AIEntityDeletedNotification<T>`, in `Umbraco.AI.Core/Models/Notifications/`) because its own entities save one at a time. `ContentCheckPolicy` and the new folder entity (§3 below) save the same way — one at a time, through their own service — so the same reasoning applies here, not the CMS-native shape. Saving/Saved carry the full entity; Deleting/Deleted carry only its identity (`EntityKey`, not the full entity — nothing left to load by the time it's gone).
- **Deploy Refresher handling:** `AIGuardrailSavedDeployRefresherNotificationAsyncHandler` and `AIGuardrailDeletedDeployRefresherNotificationAsyncHandler` (`Umbraco.AI.Deploy/src/Umbraco.AI.Deploy/NotificationHandlers/`) confirm exactly what [Deploy Integration](deploy-integration-spec.md) already assumed: Saved writes/updates the on-disk artifact (via `IServiceConnectorFactory` + `IDiskEntityService.WriteArtifactsAsync` + signature update), Deleted removes it (`IDiskEntityService.DeleteArtifacts` + signature clear). No surprises there — this doc formalizes the notification *names* those handlers subscribe to.
- **No "evaluation completed" precedent exists anywhere in Umbraco.AI.** Checked directly: `IAIOperationTracker` is a plain internal service with no notification published from it; nothing named `AIGuardrailEvaluationCompletedNotification` or similar exists. The run-completion notifications in §4 below aren't ported from anything — they're new ground for this project, same as the rubric concept itself (see [research.md](research.md) §4's closing line). Worth being explicit about, since everything else in this project has had a precedent to mirror or correct.
- **Folder notifications, precedent from Umbraco.Automate:** its `WorkspaceGroup` (Automate's folder entity for organizing automations — the direct precedent for §3 below) fires its own parallel `WorkspaceGroupSavingNotification`/`SavedNotification`/`DeletingNotification`/`DeletedNotification` set, distinct from the automation entity's own notifications, using the standard `CancelableObjectNotification<T>`/`ObjectNotification<T>` CMS base classes rather than a bespoke pair. A folder gets its own notification pair here too, for the same reason: it's a distinct persisted entity with its own lifecycle, not a property of the policy.

## 1. Base shape

Two axes, same as the precedent: cancelable-before vs. stateful-after, and full-entity vs. identity-only. Defined once in Core, reused by both entities in §2 and §3:

```csharp
public abstract class ContentCheckEntitySavingNotification<T> : CancelableNotification
{
    public required T Entity { get; init; }
}

public abstract class ContentCheckEntitySavedNotification<T> : StatefulNotification
{
    public required T Entity { get; init; }
}

public abstract class ContentCheckEntityDeletingNotification<T> : CancelableNotification
{
    public required Guid EntityKey { get; init; }
}

public abstract class ContentCheckEntityDeletedNotification<T> : StatefulNotification
{
    public required Guid EntityKey { get; init; }
}
```

`EntityKey`, not `EntityId` — Umbraco.AI's own version uses `EntityId` because its entities are Id-keyed; every persisted entity in this project is Key-keyed by design (the same Id/Key/Alias discipline [Policy Resolution](policy-resolution-spec.md) applies to `ContentTypeKey`/`StartNodeKey`), so the notification field name follows the same convention rather than copying the precedent's field name verbatim.

## 2. `ContentCheckPolicy` CRUD notifications

```csharp
public sealed class ContentCheckPolicySavingNotification : ContentCheckEntitySavingNotification<ContentCheckPolicy>;
public sealed class ContentCheckPolicySavedNotification : ContentCheckEntitySavedNotification<ContentCheckPolicy>;
public sealed class ContentCheckPolicyDeletingNotification : ContentCheckEntityDeletingNotification<ContentCheckPolicy>;
public sealed class ContentCheckPolicyDeletedNotification : ContentCheckEntityDeletedNotification<ContentCheckPolicy>;
```

Fired by `ContentCheckPolicyService` around its own save/delete calls (repository access pattern — nothing outside the service touches the repository directly, so nothing outside the service is a second place these could fire from).

**Primary subscriber: the Deploy Refresher** ([Deploy Integration](deploy-integration-spec.md) §2) — `ContentCheckPolicySavedDeployRefresherNotificationAsyncHandler` writes/updates the on-disk artifact; `ContentCheckPolicyDeletedDeployRefresherNotificationAsyncHandler` removes it. Direct port of the two Guardrail handler classes, now with real names to implement against instead of a description.

## 3. `ContentCheckPolicyFolder` CRUD notifications

New entity, driven by the tree/folder UI requirement (see [Management API & UI](management-api-ui-spec.md) and [Policy Resolution](policy-resolution-spec.md) §5 for the model). Gets its own parallel notification set, not folded into the policy's — same reasoning Automate's `WorkspaceGroup` already validated: a folder is its own persisted entity with its own lifecycle, independent of any policy that happens to live inside it.

```csharp
public sealed class ContentCheckPolicyFolderSavingNotification : ContentCheckEntitySavingNotification<ContentCheckPolicyFolder>;
public sealed class ContentCheckPolicyFolderSavedNotification : ContentCheckEntitySavedNotification<ContentCheckPolicyFolder>;
public sealed class ContentCheckPolicyFolderDeletingNotification : ContentCheckEntityDeletingNotification<ContentCheckPolicyFolder>;
public sealed class ContentCheckPolicyFolderDeletedNotification : ContentCheckEntityDeletedNotification<ContentCheckPolicyFolder>;
```

**Same primary subscriber, same reason: the Deploy Refresher.** A folder is schema-level organizational config, exactly the same *kind* of thing [Deploy Integration](deploy-integration-spec.md) §1 already argued for the policy itself — it should move between environments as a disk artifact, in a PR, not silently. `ContentCheckPolicyFolderSavedDeployRefresherNotificationAsyncHandler` / `...DeletedDeployRefresherNotificationAsyncHandler` are a second pair of handlers alongside §2's, not a variation on the same one — Deploy Integration's scope (§4 there) now covers both entities.

## 4. Run-completion notifications — two tiers, matching the two layers that already exist

[Core Contract](core-contract-spec.md)'s runner only ever sees one snapshot at a time; [Policy Resolution](policy-resolution-spec.md) is the layer that discovers many snapshots (root plus every cascaded element) and calls the runner once per snapshot. A single notification can't serve both "something worth logging just happened" and "the whole triggered run is done, branch on it" — those are genuinely different granularities, not the same event described two ways, so this is two notifications, fired from the two places that already have the right information, not one notification with an ambiguous scope.

```csharp
// Fired by IContentCheckRunner.RunAsync itself, once per call — one snapshot, one report.
// Low-level extensibility seam: logging, auditing, analytics, or a per-block Automate workflow
// that genuinely wants to react to one nested element's own result. Automate's Trigger direction
// (see Automate Integration §"Trigger") can subscribe to this directly for that case, but it is
// not the notification a "tell me if this page failed" workflow trigger should be built against —
// see the aggregate below for that.
public sealed class ContentCheckReportCompletedNotification : StatefulNotification
{
    public required ContentCheckReport Report { get; init; }
    public required Guid ContentKey { get; init; }
    public required string ContentTypeAlias { get; init; }
    public string? Culture { get; init; }
    public string? Segment { get; init; }
}

// Fired by the resolution/orchestration layer (Policy Resolution) once a full triggered run
// finishes — root snapshot plus every cascaded element snapshot discovered underneath it,
// aggregated. This is the one a page-level "did anything fail" Automate trigger, or the
// Action's branching output (see Automate Integration), actually wants.
public sealed class ContentCheckRunCompletedNotification : StatefulNotification
{
    public required Guid RootContentKey { get; init; }
    public required string RootContentTypeAlias { get; init; }
    public required IReadOnlyList<ContentCheckReportCompletedNotification> Reports { get; init; }
    // Overall-status field intentionally not shown here yet — depends on the still-open
    // "overall status" shape (Core Contract §4, Automate Integration §"Small, prompted addition").
    // Whatever that resolves to becomes a property on this notification, not a reason to delay
    // shipping the notification itself with the fields already known to be needed.
}
```

Neither is cancelable — running checks and reporting the result isn't a mutation anything downstream should be able to veto; there's nothing to prevent. That's also why no pre-run `-ing` counterpart exists for either: the CRUD notifications in §2/§3 have a cancelable "-ing" half because saving or deleting an entity is a mutation worth being able to stop; starting a check run isn't. Considered and dropped for lack of a driving use case — recorded here so it isn't quietly reintroduced by habit later, the same discipline [Core Contract](core-contract-spec.md) §6 already applies to its own cut list.

**This is a real seam, not a formality — Automate is the first subscriber, not the only intended one.** [Automate Integration](automate-integration-spec.md)'s Trigger direction is what motivated writing this section, but per that spec's own framing, the notification has no idea Automate exists; anything wanting to react to a completed run (a custom logging sink, a dashboard, a future second bridge package) subscribes the same way.

**Not yet resolved by this doc, on purpose:** the overall-status shape referenced above. That's Core Contract's open item, not renamed or narrowed into a notification-specific question here — this doc only commits to *where* that field will eventually land once it exists.

## 5. Subscriber summary

| Notification | Primary subscriber | What it does |
|---|---|---|
| `ContentCheckPolicySavedNotification` / `Deleted` | [Deploy Integration](deploy-integration-spec.md) Refresher | Write/update or remove the policy's on-disk artifact |
| `ContentCheckPolicyFolderSavedNotification` / `Deleted` | [Deploy Integration](deploy-integration-spec.md) Refresher | Write/update or remove the folder's on-disk artifact |
| `ContentCheckReportCompletedNotification` | General seam (no built-in v1 subscriber; [Automate Integration](automate-integration-spec.md) optionally, per-snapshot) | Logging/auditing/analytics; fine-grained workflow triggers |
| `ContentCheckRunCompletedNotification` | [Automate Integration](automate-integration-spec.md) Trigger bridge | Starts a configured workflow when a run matches its filter |

The `Saving`/`Deleting` cancelable halves of §2/§3 have no specified v1 subscriber — they exist because the base shape in §1 provides them symmetrically (same as CMS's own convention), not because a concrete cancelling consumer is already planned. Recorded rather than omitted, so a future need (permission check before delete, say) has somewhere obvious to attach.
