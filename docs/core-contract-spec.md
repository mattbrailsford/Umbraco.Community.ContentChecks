# Core Contract — `IContentCheck`, config, runner, caching

**Status:** Finalized (2026-08-05), revised after a second look. Phase 1 built and passing its own tests (2026-08-06). The `IContent`-backed integration test gap closed 2026-08-08 (see §7) — the 14-finding review gate is the one real gap still open before this is considered done.
**Part of:** [Overview](overview.md). See also [Policy Resolution](policy-resolution-spec.md) for how checks get attached to content and discovered at runtime — that's additive on top of this, not a rework of it.

Superseded an earlier draft after reading the actual code it was based on secondhand: `Umbraco.AI.Core.Guardrails.Evaluators` (the real `IAIGuardrailEvaluator`, `AIGuardrailEvaluatorBase<TConfig>`, `AIGuardrailResult`, `AIGuardrailRule`, `ContainsGuardrailEvaluator`, `LLMGuardrailEvaluator`) and `Umbraco.AI.Core.EditableModels`. Every decision below either **reuses** a proven shape, **adapts** it for a real difference between the two domains, or **cuts** something the earlier draft copied by reflex without a concrete need. §5 below lists every cut with the reasoning.

---

## 1. Four corrections folded in along the way

Kept rather than smoothed over, because the reasoning that killed each earlier attempt is worth keeping:

1. **Checks don't fix things — cut entirely.** The first pass added `IFixableContentCheck`/`ExecuteFixAsync`, borrowed from Umbraco's Health Check `ExecuteAction`. Wrong transplant: a Health Check fix mutates a config value or a file — low-stakes, unversioned, unpermissioned. A "fix" to a content property is a **content edit** — it has to go through Umbraco's governed editing pipeline (`IContentEditingService`, versioning, publish validation, permissions), which a validation plugin has no business owning. A check's job is to *evaluate*, full stop; it stays side-effect-free. If an AI agent or an editor wants to act on a result, they edit the content the normal way, through the normal service, with the normal audit trail — reading the check's `Message` to know what to change. Nothing in the contract represents "fix" as a capability.
2. **The attribute and the generic base class are shown as part of the contract, not split into a separate subsection.** The first pass presented a bare `IContentCheck` interface in isolation and only introduced `[ContentCheck(...)]` and `ContentCheckBase<TConfig>` afterward, as if they were an implementation detail. They aren't: in the real Guardrails code, nobody hand-implements `IAIGuardrailEvaluator` directly with a hardcoded `Alias`/`Name` — every real evaluator (`ContainsGuardrailEvaluator`, `LLMGuardrailEvaluator`) is declared via the attribute and inherits the generic base class. That combination *is* "how you write a check." Below, all three appear together.
3. **`Cost` and `Kind` are independent properties again — un-collapsing a simplification that didn't survive a counterexample, and `Kind`'s members went through three attempts before landing right:**
   - First: `Deterministic`/`AiJudged` on one enum. Wrong — `Deterministic` names a *property* (is the output predictable), `AiJudged` names a *mechanism* (how the check runs); pairing a property-word against a mechanism-word as opposites doesn't hold.
   - Second: `RuleBased`/`AiJudged` — both mechanism words this time, matching Guardrails' `CodeBased`/`ModelBased`. Wrong differently: too specific. A check that calls a slow, unreliable third-party API (a grammar service, a link-checker) is neither a fixed local rule nor an AI judgment — nowhere for it to go.
   - Landed: `Deterministic`/`Nondeterministic` — a correctly-matched property/property pair (true antonyms), and mechanism-agnostic (a third-party API, an AI judge, anything whose output can vary run-to-run all fit `Nondeterministic` without the type naming *why*). This also earns its keep beyond naming correctness: `Nondeterministic` is exactly the property the *cache* (§4) needs to know before it can safely reuse a stored result. `Kind` and `Cost` stay two independent properties regardless of which words each took: the underlying mistake was assuming "costs money" and "isn't a fixed rule" always co-occur, which they don't in either direction — a slow third-party deterministic lookup is costly but not a judgment call; a tiny embedded classifier could be a judgment call that's fast and free. `Cost { Free, Metered }` is what the *runner* orders and short-circuits on; `Kind { Deterministic, Nondeterministic }` is what the *cache* and the *UI/trust story* (finding G1) key off. This distinction was validated with a real, concrete example later, independently — see the [markup-checks exploration doc](markup-checks-exploration.md)'s §8: rendering a page for a markup-based check is expensive but not AI-judged, exactly the "costly but not a judgment call" case this reasoning predicted.
4. **`Config`, not `Settings`, throughout — matching Umbraco.AI's own naming exactly, not a rename of it.** An earlier pass renamed the ported schema infrastructure to "Settings" vocabulary but let that word leak into the interface itself as `GetSettingsSchema()`, sitting right next to `ConfigType` on the same interface — two different root words for two clearly-related members, on top of `ContentCheckConfig`, `TConfig`, and `ResolveConfig` already using "Config" everywhere else. The real `IAIGuardrailEvaluator` doesn't have this problem: its method is `GetConfigSchema()`, not `GetSettingsSchema()` — Umbraco.AI already keeps "Config" as the interface-facing word and reserves "EditableModel" purely for the schema *infrastructure's own class names*, never for a method a check author sees. Followed exactly: `GetConfigSchema()`, `ContentCheckConfigSchema`, `IContentCheckConfigSchemaBuilder`, `IContentCheckConfigResolver`. "Settings" doesn't appear anywhere in this contract.

## 2. The contract

```csharp
// The interface is thin and non-generic — it's what the runner and collection builder see.
// Nobody implements this directly; see ContentCheckBase<TConfig> below.
public interface IContentCheck : IDiscoverable
{
    string Alias { get; }
    string Name { get; }
    string Description { get; }
    ContentCheckCost Cost { get; }             // Free | Metered — what the runner orders/short-circuits on
    ContentCheckKind Kind { get; }             // Deterministic | Nondeterministic — cache + UI disclosure (finding G1)

    Type? ConfigType { get; }
    ContentCheckConfigSchema? GetConfigSchema();

    Task<ContentCheckResult> EvaluateAsync(
        ContentCheckContext context, ContentCheckConfig config, CancellationToken cancellationToken);
}

public enum ContentCheckCost { Free, Metered }
public enum ContentCheckKind { Deterministic, Nondeterministic }

// Declares Alias/Name/Cost/Kind — mirrors [AIGuardrailEvaluator(id, name) { Type = ... }], split
// into two independent properties instead of Guardrails' one (see correction 3 above for why).
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class ContentCheckAttribute(string alias, string name) : Attribute
{
    public string Alias { get; } = alias;
    public string Name { get; } = name;
    public ContentCheckCost Cost { get; set; } = ContentCheckCost.Free;                    // opt in to Metered
    public ContentCheckKind Kind { get; set; } = ContentCheckKind.Deterministic;           // opt in to Nondeterministic
}

// What you actually write a check by inheriting — direct port of AIGuardrailEvaluatorBase<TConfig>,
// plus one addition beyond that precedent (see the note below the block): a typed EvaluateAsync
// overload is what a check actually overrides; the interface-facing one is a concrete proxy that
// resolves and defaults the config once, so every check stops repeating that step by hand.
// One constructor only (infrastructure-DI) — no [Obsolete] legacy overload; nothing to be
// backward-compatible with in a greenfield package.
public abstract class ContentCheckBase<TConfig> : IContentCheck
{
    public string Alias { get; }
    public string Name { get; }
    public abstract string Description { get; }
    public ContentCheckCost Cost { get; }
    public ContentCheckKind Kind { get; }
    public Type? ConfigType => typeof(TConfig);

    protected ContentCheckBase(IContentCheckInfrastructure infrastructure) { /* reads attribute, builds Lazy<schema> */ }

    public ContentCheckConfigSchema? GetConfigSchema() => /* lazy schema */ null;
    protected TConfig? ResolveConfig(ContentCheckConfig config) => /* schema-backed resolve */ default;

    // Implements IContentCheck — concrete, not abstract. Resolves the raw config once, falls
    // back to `new TConfig()` (the same instance the schema's own DefaultValue fields read
    // from) if none was stored, then hands off to the typed overload below.
    public Task<ContentCheckResult> EvaluateAsync(
        ContentCheckContext context, ContentCheckConfig config, CancellationToken cancellationToken)
        => EvaluateAsync(context, ResolveConfig(config) ?? CreateDefaultConfig(), cancellationToken);

    // What a check actually overrides — an overload, not the interface member itself, so the
    // compiler only lets `override` land on this one.
    protected abstract Task<ContentCheckResult> EvaluateAsync(
        ContentCheckContext context, TConfig config, CancellationToken cancellationToken);

    private static TConfig CreateDefaultConfig() => (TConfig)Activator.CreateInstance(typeof(TConfig))!;
}

public sealed class ContentCheckContext
{
    // finding L — a resolved snapshot, never an id. Lets a Draft Set / unsaved editor
    // state / trunk content all be validated through one path.
    public required IContentCheckSnapshot Content { get; init; }
    public string? Culture { get; init; }      // finding G
    public string? Segment { get; init; }
}

// Mirrors AIGuardrailConfig's shape, minus its Deserialize<T>() convenience method — see the
// note below the block for why that method didn't earn its keep here.
public sealed class ContentCheckConfig
{
    public JsonElement? Config { get; init; }
}

// finding G3 — modelled on Umbraco's real HealthCheckStatus.ResultType, extended with the two
// states site health doesn't need but content quality does (NotApplicable, NotEvaluated).
public enum ContentCheckStatus { Pass, Warn, Fail, NotApplicable, NotEvaluated }
public enum ContentCheckNotEvaluatedReason { ShortCircuited, Error }

public sealed class ContentCheckResult
{
    public required ContentCheckStatus Status { get; init; }
    public required string Message { get; init; }     // the only place a suggested fix is expressed —
                                                        // as prose an editor or agent acts on themselves
    public double? Score { get; init; }                                   // rubrics produce a continuum
    public double? Threshold { get; init; }                               // first-class: the results UI's
                                                                           // score-vs-threshold bar needs it
                                                                           // without parsing per-check metadata
    public ContentCheckNotEvaluatedReason? NotEvaluatedReason { get; init; }
    public IReadOnlyList<ContentCheckTarget>? Targets { get; init; }      // finding I
}

// No block/element path here — deliberately. See Policy Resolution's cascading section: a check
// is always handed a snapshot of exactly one thing (the root document, or one nested element), so
// PropertyAlias is always unambiguous — "a property on whatever snapshot this check was given."
// Which root property and which chain of ancestor block keys led to that snapshot is context the
// *resolution layer* already has for free from walking the tree — it belongs on the report entry
// the runner produces, not on a value a check author has to construct by hand.
public sealed class ContentCheckTarget
{
    // Every field optional — a target can be "the whole item" (all null) up to
    // "this exact property, this variant" (all populated).
    public string? PropertyAlias { get; init; }
    public string? Culture { get; init; }
    public string? Segment { get; init; }
}

// Mirrors Umbraco's own IContent.GetValue<TValue>(alias, culture, segment) signature exactly,
// so an adapter over a real IContent is a one-line delegation, not a translation layer.
public interface IContentCheckSnapshot
{
    Guid Key { get; }
    string ContentTypeAlias { get; }
    int? VersionId { get; }   // null when this snapshot has no persisted version yet (unsaved
                               // editor state, a Draft Set's in-memory pending state) — §5's cache
                               // falls back to hashing values when this is null
    bool HasProperty(string alias);   // distinct from GetValue returning null — resolves
                                       // "no such property" (→ NotApplicable) vs "empty value" (→ Fail)
    TValue? GetValue<TValue>(string alias, string? culture = null, string? segment = null);
}
```

**Added after the contract was first marked finalized, before any code was written against it — caught by checking real Umbraco source rather than assumed.** §5's cache design says "the content item's own version Guid" throughout; reflecting over the actual `Umbraco.Cms.Core.Models.Content` class shows `VersionId` (and `PublishedVersionId`) are `int`, not `Guid` — there is no Guid-typed version identifier anywhere on `IContent`. `IContentCheckSnapshot` needed a way to expose that identity regardless, so `VersionId` (nullable `int`) was added directly to the interface — a real, small addition to an already-"finalized" contract, made deliberately rather than silently, exactly the discipline the "one-way door" framing above asks for. Every occurrence of "version Guid" elsewhere in this doc and in the [Overview](overview.md) findings table means this `int? VersionId`, not a Guid — read literally, "Guid" was simply wrong.

**Second addition after Phase 1 was built, caught by noticing repetition rather than a wrong assumption: `ContentCheckConfig.Deserialize<T>()` and the every-check boilerplate it left behind.** `AIGuardrailConfig.Deserialize<T>()` was ported verbatim as a convenience method — but nothing ever called it. Every real check goes through `ContentCheckBase<TConfig>.ResolveConfig`, never the raw config's own `Deserialize<T>()`; the method only earns its keep for a hand-rolled `IContentCheck` implementer, and `IContentCheck`'s own docs already say nobody does that. Cut. Its removal exposed a second, more valuable thing: every proving check's `EvaluateAsync` opened with the identical line `ResolveConfig(config) ?? new XyzConfig()` — resolve the raw config, default it if absent. That line belongs on the base class, not repeated in every check. `ContentCheckBase<TConfig>.EvaluateAsync(ContentCheckContext, ContentCheckConfig, ...)` is no longer abstract; it's a concrete proxy that resolves and defaults once, then calls a new `protected abstract EvaluateAsync(ContentCheckContext, TConfig, ...)` overload — an overload, not an override, so the compiler itself only lets a check's `override` keyword land on the correctly-typed one. The default instance comes from `Activator.CreateInstance(typeof(TConfig))`, deliberately not a `where TConfig : new()` constraint — §3's config schema builder already creates a default `TConfig` via reflection for the exact same reason (reading each field's `DefaultValue` from the type's own property initializers), and adding a formal constraint here would contradict the reasoning already recorded for why `ResolveConfig` avoids one. An unconfigured rule now behaves exactly as its own schema already advertises, not a separately-invented fallback.

**Class, not record, everywhere in this contract.** `AIGuardrailResult`/`AIGuardrailRule`/`AIGuardrail` are all `sealed class` with `{ get; init; }` (some `required`), never positional records. That's deliberate on Umbraco.AI's side, not incidental: third-party developers *construct* these types (a check returns `ContentCheckResult`, an evaluator returns `AIGuardrailResult`), and a positional record's constructor is a breaking-change trap — adding a new parameter later breaks every existing call site. Init-property classes let a new optional property get added later for free. Followed exactly here.

**`Cost` and `Kind` live directly on the interface, not wrapped in a `Metadata` object.** Reasoning that interfaces are harder to extend than classes doesn't hold: `IAIEditableModelResolver.ResolveModel(Type, ...)` is a **default interface method** added to a public interface after the fact specifically so old implementers keep compiling — Umbraco.AI's own code proves default interface members are the sanctioned extension path here, not wrapper objects. So both classification properties sit directly on `IContentCheck`, roughly where Guardrails puts the single equivalent `Type` property on `IAIGuardrailEvaluator`, and any future classification property arrives the same way theirs would.

**Two properties, not one.** Guardrails gets away with a single `Type` enum (`CodeBased`/`ModelBased`) because in its domain the two axes (cost, and whether the output is reproducible) really do always co-occur. This domain doesn't guarantee that, so `Cost` and `Kind` stay independent: a check declares each separately, and most checks will naturally be `Free`+`Deterministic` or `Metered`+`Nondeterministic`, but a `Metered`+`Deterministic` check (a slow but reliably-repeatable third-party lookup) is representable without a contract change if one ever gets written.

**`Scope` dropped entirely for v1** (finding F). Pre-declaring `Item`/`Branch`/`Set` to avoid "foreclosing" broader scopes later solves nothing — enum members are additive and non-breaking regardless. The real blocker for Branch/Set was never a missing enum value; it's that a branch-scoped check needs a different method shape entirely (many snapshots in, not one). When that's built, it arrives as a new opt-in interface, same pattern as the Fixable one that got cut. (Open question 5 in the [Overview](overview.md).)

**Config is its own parameter, not folded into `ContentCheckContext.Settings`.** An earlier pass had context carrying `IReadOnlyDictionary<string, object?> Settings` — an untyped bag — alongside the content to check. The real precedent (`EvaluateAsync(string content, history, AIGuardrailConfig config, ct)`) keeps the evaluation subject and the check's configuration as two separate, differently-shaped parameters. `ContentCheckContext` now holds only what's being checked; `ContentCheckConfig` (mirroring `AIGuardrailConfig`'s shape) holds the raw config, typed and defaulted via `ContentCheckBase<TConfig>`'s own `EvaluateAsync` proxy (see the note above) rather than a `Deserialize<T>()` method on the config type itself.

**Deviation, kept deliberately: `Alias`, not `Id`.** Umbraco.AI's own convention splits the two — `Id` for a code-registered plugin type (`IAIGuardrailEvaluator.Id`), `Alias` for a DB-persisted user-created entity (`AIGuardrail.Alias`, `AIProfile.Alias`). ContentChecks uses `Alias` for both the code-registered check type and the check-policy record. That's a real, intentional departure from the precedent being mirrored everywhere else in this section — justified because Umbraco.AI's Id/Alias split exists to disambiguate *many* entity kinds across a large product; a small standalone package is clearer with one word for "the name you refer to this by," full stop.

**Fail-open on error is the opposite of the nearest precedent — on purpose.** `LLMGuardrailEvaluator.EvaluateAsync`'s catch block treats a provider failure as `Flagged = true` — fail closed, because for chat safety, blocking on an error is the safe default. That is exactly wrong for content quality: fail-closed-on-error here means an AI outage blocks *every* publish behind a blocking check. `NotEvaluated` exists specifically so this domain fails open by default (finding G3) — a deliberate divergence from the code being mirrored, not an oversight.

**Registration mirrors Guardrail evaluators, not middleware.** An earlier draft said "Umbraco `OrderedCollectionBuilder` pattern" — wrong; that's for middleware *pipelines* (each wraps the next). Check types are independently-registered plugins, discovered the same way guardrail evaluators and providers are: `IContentCheck : IDiscoverable`, the `[ContentCheck(...)]` attribute above, and `LazyCollectionBuilderBase<ContentCheckCollectionBuilder, ContentCheckCollection, IContentCheck>` — auto-discovered, with `Add<T>()`/`Exclude<T>()` available in a Composer for manual overrides. Direct port of `AIGuardrailEvaluatorCollectionBuilder`.

## 3. Check config — schema-driven, ported and trimmed

Settles open question 6 in the [Overview](overview.md) (partially — the escape-hatch idea there is still deferred).

**Decided: port `Umbraco.AI.Core.EditableModels`, trimmed of the parts this package doesn't need.** Read the actual source (`AIEditableModelSchema`, `AIEditableModelField`, `AIEditableModelFieldAttribute`, `AIEditableModelSchemaBuilder`, `AIEditableModelResolver`) to see exactly what's structural (port verbatim) versus what exists only to serve Umbraco.AI's own concerns (cut — see §5 for the full list). Renamed to `ContentCheckConfigSchema`, `IContentCheckConfigSchemaBuilder`, `IContentCheckConfigResolver`, `[ContentCheckField]` — **not** "Settings" vocabulary (§1's correction 4). Umbraco.AI's own "EditableModel" name is generic on purpose — it serves provider settings, guardrail config, *and* test grader config in one system — but its *interface-facing method* is still called `GetConfigSchema()`, matching everything else on the interface. Same convention, applied consistently here.

**Single attribute name, no legacy alias.** Umbraco.AI carries both `AIEditableModelFieldAttribute` and a short alias `AIFieldAttribute` for the same thing — almost certainly a rename that kept the old name for compatibility. ContentChecks is greenfield, so it ships with one name (`[ContentCheckField]`) from day one; there's no history to be compatible with yet.

The pattern, ported:

1. A check author writes one plain C# class for their config — `RequiredPropertyCheckConfig { [ContentCheckField(Label = "Property alias")] public string PropertyAlias { get; set; } = ""; }` — a mutable class with a parameterless constructor (the resolver requires `TModel : class, new()`, matching the real `IAIEditableModelResolver`).
2. `IContentCheck.ConfigType` returns that type; `GetConfigSchema()` reflects over it once, lazily, exactly like `AIGuardrailEvaluatorBase`'s `Lazy<AIEditableModelSchema?>`.
3. The Management API exposes the schema per registered check type (see [Management API & UI](management-api-ui-spec.md)).
4. A generic frontend component renders any schema — either a straight port of `uai-model-editor` or a fresh build against the identical field shape (`Key`, `Label`, `Description`, `EditorUiAlias`, `EditorConfig`, `DefaultValue`, `SortOrder`, `ValidationRules`, `Group` — `IsSensitive` dropped, see §5). Either way, **a new check type ships with zero frontend code.**
5. The resolved config is stored as JSON (`ContentCheckConfig.Config : JsonElement?`), deserialized through the schema at evaluation time via a `ResolveConfig<TConfig>()` helper on the `ContentCheckBase<TConfig>` base class — same role as `AIGuardrailEvaluatorBase.ResolveConfig`, minus the configuration-variable substitution machinery (§5). A check author never calls this directly: the base class's `EvaluateAsync` proxy calls it automatically before handing a check its already-typed, already-defaulted config (see §2's note on this).

This is still why `Nondeterministic`/`Metered` check config (see [AI Integration](ai-integration-spec.md) — judge profile alias, rubric text, threshold) needs no bespoke UI: it's just another `TConfig` class.

**Resolved by the packaging decision in the [Overview](overview.md):** `Umbraco.Community.ContentChecks.AI` lives in the same repo/solution as Core, so it project-references Core's ported config infra directly, same as every other check type.

## 4. Runner

`IContentCheckRunner.RunAsync(ContentCheckRunRequest, CancellationToken) → ContentCheckReport`

```csharp
public sealed class ContentCheckRunRequest
{
    public required IContentCheckSnapshot Content { get; init; }
    public string? Culture { get; init; }
    public string? Segment { get; init; }
    public required IReadOnlyList<ResolvedContentCheckRule> Rules { get; init; }
}

public enum ContentCheckRuleMode { Advisory, Blocking }

// The runtime-resolved counterpart of a persisted ContentCheckPolicyRule (see Policy Resolution)
// — same concept, one lifecycle stage later: the check alias has been resolved to a real
// IContentCheck instance.
public sealed class ResolvedContentCheckRule
{
    public required IContentCheck Check { get; init; }
    public required ContentCheckConfig Config { get; init; }
    public ContentCheckRuleMode Mode { get; init; } = ContentCheckRuleMode.Advisory;
}

// A log row describing what happened when one rule ran — not the same concept as the rule itself,
// so it keeps "Entry" rather than picking up the "Rule" naming (a report is a list of entries).
public sealed class ContentCheckReportEntry
{
    public required string CheckAlias { get; init; }
    public required ContentCheckRuleMode Mode { get; init; }
    public required ContentCheckResult Result { get; init; }
    public TimeSpan Elapsed { get; init; }
}

public sealed class ContentCheckReport
{
    public required IReadOnlyList<ContentCheckReportEntry> Entries { get; init; }
}
```

Ordering keys off `Cost`, not `Kind` — whether a check's output is reproducible is a cache/disclosure concern; whether it's cheap to run is what the runner actually needs to schedule around (findings G2, N):

1. Run every `Free` rule first, always.
2. If any rule that is both `Blocking` **and** `Free` produced `Fail`, every `Metered` rule short-circuits straight to `NotEvaluated(ShortCircuited)` — no expensive call is made, LLM or otherwise.
3. Otherwise every `Metered` rule runs, concurrently, up to a per-run cap.
4. `Elapsed` on each report entry makes cost visible without a separate accounting mechanism; total metered-call count is just `Entries.Count(e => e.Check.Cost == ContentCheckCost.Metered && e.Result.Status != ContentCheckStatus.NotEvaluated)`.

**`NotEvaluatedPolicy` (fail-open/fail-closed) is not part of the runner.** It was drafted into `ResolvedContentCheckRule` originally, but the runner's only job is to run checks and report faithfully what happened — deciding whether a `NotEvaluated` blocking rule should actually stop a publish is a question for the *caller* ([Triggers](triggers-spec.md)'s publish interception), not the runner. `NotEvaluatedPolicy` lives on the persisted `ContentCheckPolicyRule` (see [Policy Resolution](policy-resolution-spec.md)) and is read by the triggers' gating logic against the report the runner already returned. Keeping it out of the runner's request shape is a straightforward scope cut, not a capability loss.

**This runner stays unchanged even as [Policy Resolution](policy-resolution-spec.md) adds element-type cascading and composition matching on top of it** — it was already correctly scoped to "one snapshot, one set of resolved rules, one report." Everything more elaborate calls this same runner once per discovered snapshot and aggregates.

**Resolved by the Phase 3 pre-design, not by a change here:** `ContentCheckReport` itself still has no notion of an overall status — only per-entry statuses, and that stays true; a single-snapshot report is still exactly "one snapshot, one set of resolved rules." The overall-status question this item raised lands one layer up instead: [Policy Resolution](policy-resolution-spec.md) §6's `ContentCheckAggregateReport` (the multi-snapshot type `IContentCheckEvaluationService` produces) carries two computed properties — `OverallStatus` (worst-status-wins, for display) and `HasBlockingFailure` (for gating/branching) — because "how bad is it" and "should this block" turned out to be two different questions, not one, prompted by [Automate Integration](automate-integration-spec.md)'s original ask.

**The runner fires a notification when it finishes.** `RunAsync` publishes `ContentCheckReportCompletedNotification` (carrying the `ContentCheckReport` it just produced, plus the snapshot's key/content-type-alias/culture/segment for filtering) after every call — see [Notifications](notifications-spec.md) §4 for the full shape and why this is deliberately the low-level, per-snapshot half of a two-tier design, not the notification a page-level Automate trigger should subscribe to.

## 5. Caching

**Key off the content item's own version identity, not a hash of the snapshot, whenever one exists.** Umbraco already stamps every save with a version id (`int? VersionId`) — the content hasn't changed if the version hasn't. Hashing the snapshot's actual property values to detect a change is redundant work when Umbraco already tracked that for free, and it's also strictly worse: two different versions with identical content would hash the same and look cache-fresh when they're actually two distinct saves, where checking the version id (`int? VersionId`) directly has no such ambiguity. This directly serves the request behind this decision — **a bulk sweep across many items only re-runs checks on items that actually changed since the last run**; anything whose current version already has a fresh cached result is skipped outright, `Metered`/AI-judged calls included. That's the concrete payoff, not just a performance nicety: a full-site backtest or a scheduled Automate sweep ([Automate Integration](automate-integration-spec.md)) becomes proportional to what changed, not to the size of the site, every time it runs after the first.

**One real gap this doesn't cover, worth being explicit about rather than glossing over:** a version id (`int? VersionId`) only exists for something that's actually been saved. [Finding L](overview.md) is why `ContentCheckContext` carries a resolved snapshot rather than an id in the first place — an unsaved editor draft or a Draft Set's in-memory pending state has no version yet to key on. **Corrected while actually implementing this, not just while designing it:** the original plan was to fall back to hashing the snapshot's values for that case — but `IContentCheckSnapshot` has no generic way to enumerate its own values (only `HasProperty`/`GetValue<TValue>` for a *specific*, caller-known alias), so there is nothing honest to hash from the runner's side without hardcoding assumptions about which properties matter. **The cache is skipped entirely instead** — no lookup, no write — for any snapshot with a null `VersionId`. Caching barely helps there anyway, since the whole point of that path is content that's actively changing between every keystroke, not content sitting still between repeated runs.

`IContentCheckResultCache`, key = content item's version id (`int? VersionId`) + snapshot key (for a nested element, since one version can contain many cacheable snapshots) + culture + segment + check alias + check-assembly fingerprint + resolved-config hash. No key is built at all when `VersionId` is null — see above.

**Doesn't resolve, and isn't meant to resolve, [open question 7](overview.md)'s cascading-cost concern.** This makes the *second and later* run over a page with twenty Testimonial blocks cheap once nothing's changed — it does nothing for the *first* run, which still pays for all twenty evaluations up front. Batch/cap/accept for that first-run cost is still an open, separate decision.

**No `Version` property on `IContentCheck`.** The original draft's cache key included a manually-maintained "check version," which asks every check author to remember to bump a number whenever their check's logic changes — easy to forget, and a silent cache-staleness bug when they do. The assembly's own version (`checkType.Assembly.GetName().Version`) gives the same cache-busting guarantee automatically, at the cache layer, with zero contract surface and zero reliance on developer discipline.

**No separate "judge fingerprint" concept in the generic cache key.** For a `Nondeterministic` check, the judge profile alias and rubric text are just fields on that check's own `TConfig` — they're already inside "resolved-config hash." Nothing AI-specific needs to be special-cased into the cache key shape. (One real nuance deferred to [AI Integration](ai-integration-spec.md), not solved generically here: an admin can swap a profile's underlying model without touching the check's config, which the config hash alone wouldn't catch. Not a v1 problem.)

## 6. What was deliberately cut

Reviewed against the real Umbraco.AI code, not against a hypothetical "what would be nice." Each of these is something the earlier draft either copied by reflex or invented without a concrete v1 need:

| Cut | Was in the draft as | Why it's gone |
|---|---|---|
| `JsonPath` / `ElementKey` / `ElementPath` on `ContentCheckTarget` | Three successive attempts to let a check point at a specific block, nested arbitrarily deep, from inside its own result | Wrong layer, not just wrong shape. A single Guid resolves correctly (Umbraco block keys are globally unique regardless of depth) but can't be traversed without a full re-walk. A path of Guids fixes traversal but is still ambiguous — `PropertyAlias` can't say whether it means "a property on the root document" or "a property on the innermost element," and a bare list of keys doesn't say *which property* to descend through at each level, since an element type can have more than one Block List/Grid property. Fixing that properly needs an ordered list of `{PropertyAlias, ElementKey}` steps — a real, coherent design, just not one worth building against a hypothetical. **The actual resolution ([Policy Resolution](policy-resolution-spec.md)): a check is always evaluated against a snapshot of exactly one thing — root document or one nested element — so it never needs to express nesting at all.** The resolution layer, which does the tree-walking, is where ancestor/location context belongs, added to the *report* it produces, not constructed by every check author by hand. |
| `IFixableContentCheck` / `ExecuteFixAsync` / `ContentCheckFixAction` | Opt-in fix capability, borrowed from Health Check's `ExecuteAction` | A fix to a content property is a governed content edit (versioning, publish validation, permissions) — not a validation plugin's job. Checks stay read-only; a suggested fix is prose in `Message`, acted on by the caller through the normal editing pipeline. |
| `ContentCheckMetadata` wrapper | Held `Cost`/`IsDeterministic`/`Scope` | `Cost` and `Kind` sit directly on `IContentCheck`; default interface methods (already Umbraco.AI's own pattern) cover future extension |
| `ContentCheckScope` enum (`Item`/`Branch`/`Set`) | Pre-declared to avoid "foreclosing" future scopes | Enum/interface additions are non-breaking; Branch/Set will need a new opt-in interface (different method shape) regardless, so pre-declaring the enum value solved nothing |
| `NotEvaluatedPolicy` on the runner's rule type | On `ResolvedContentCheckRule` | Belongs on the persisted `ContentCheckPolicyRule` ([Policy Resolution](policy-resolution-spec.md)) and the triggers' gating logic, not the runner |
| `IReadOnlyDictionary<string, object?> Settings` on context | Untyped config bag folded into `ContentCheckContext` | Replaced by a proper `ContentCheckConfig` parameter (mirrors `AIGuardrailConfig`), resolved to a typed, defaulted `TConfig` via `ContentCheckBase<TConfig>`'s own `EvaluateAsync` proxy |
| `ContentCheckConfig.Deserialize<T>()` | Ported convenience method, mirroring `AIGuardrailConfig.Deserialize<T>()` | Never called — every real check resolves config through `ContentCheckBase<TConfig>`, and `IContentCheck`'s own docs say nobody implements the raw interface directly, so nothing needs the escape hatch this method existed for |
| Configuration-variable substitution (`$Key:Path`, `$$` escaping, secret-prefix allow-listing) | Implied by "port the editable-model infra" wholesale | ~130 lines of real complexity in `AIEditableModelResolver` that exists to let provider settings reference `appsettings.json` secrets. No ContentChecks setting is a secret — the AI-judged check references an Umbraco.AI profile *by id*; the profile (in Umbraco.AI, not here) owns the actual credential. Add back only if a future check type genuinely needs one. |
| `IsSensitive` field flag + masked-editor inference | Ported implicitly with the field/attribute shape | Exists only to support the secret-substitution machinery above, which isn't needed either |
| `[Obsolete]` `StaticServiceProvider`-based constructor on the base class | N/A — would've been copied by habit | Only exists on `AIGuardrailEvaluatorBase` because its contract was already public before the infrastructure-DI constructor was added. Nothing to be backward-compatible with in a greenfield package. |
| "Judge fingerprint" as its own cache-key concept | Explicit in an earlier draft | Already covered by hashing the resolved config, since profile alias + rubric text live there |
| Manually-maintained check `Version` | Implicit "check version" in the cache key | Replaced by the check's own assembly version — same guarantee, no new contract surface, no reliance on remembering to bump it |

## 7. Phase 1 — Build checklist

This is the phase that matters. **Do not ship the contract to consumers until both proving checks exist** (finding G4).

- [x] Contract types per §2, in `Core/Checks/`: `IContentCheck`, `ContentCheckAttribute`, `ContentCheckBase<TConfig>`, `ContentCheckCost`, `ContentCheckKind`, `ContentCheckContext`, `ContentCheckConfig`, `ContentCheckStatus`, `ContentCheckResult`, `ContentCheckTarget`, `IContentCheckSnapshot`.
- [x] Port the config schema infra from Umbraco.AI's `EditableModels` into `Core/Config/` as `ContentCheckConfigSchema`, `IContentCheckConfigSchemaBuilder`, `IContentCheckConfigResolver`, `[ContentCheckField]` (§3) — **trimmed**, not a straight copy: drop configuration-variable substitution (`$Key:Path`, secret-prefix allow-listing) and `IsSensitive` entirely (§6). Just JSON round-trip + DataAnnotations validation via the schema.
- [x] `ContentCheckAttribute` + `ContentCheckBase<TConfig>` — direct port of `AIGuardrailEvaluatorBase<TConfig>` minus its `[Obsolete]` legacy constructor (nothing to be compatible with yet). Diverges from that precedent in one deliberate way, added once repetition across the two proving checks made it obvious: `EvaluateAsync` is a concrete proxy resolving/defaulting `TConfig` once, with a `protected abstract` typed overload for checks to actually implement — see §2's note.
- [x] `IContentCheckSnapshot` + an adapter over `IContent` (`ContentCheckSnapshot`) and a dictionary-backed one for content with no persisted version (`InMemoryContentCheckSnapshot`) — serves both "unsaved editing model" and "test double" roles, since no single concrete Umbraco type represents unsaved editor state generically.
- [x] `IContentCheckRunner` + ordering, short-circuit, concurrency cap, timing (§4).
- [x] `ContentCheckReportCompletedNotification`, published by the runner after every `RunAsync` call — see [Notifications](notifications-spec.md) §4.
- [x] `IContentCheckResultCache` (in-memory v1) per §5 — key includes the check's own assembly version, not a manually-maintained one, and the content's own version id (`int? VersionId`); the cache is skipped entirely (no lookup, no write) when `VersionId` is null (unsaved/Draft Set content).
- [x] `ContentCheckCollection` / `ContentCheckCollectionBuilder` as `LazyCollectionBuilderBase<,,>` + `IDiscoverable` auto-discovery — **not** `OrderedCollectionBuilder`, which is for middleware pipelines, a different shape of problem.
- [x] **Proving check A (`Cost.Free` + `Kind.Deterministic`):** required-property check. Config = property alias. Exercises `NotApplicable` (via `HasProperty`), `Fail`/`Pass`, and `Targets`. (Per-culture requirement toggle not built — not needed to prove the contract; add if a real v1 check needs it.)
- [x] **Proving check B (`Cost.Metered` + `Kind.Nondeterministic`, throwaway):** a stub rubric check with a hand-rolled fake judge that returns a score. **No Umbraco.AI dependency** — proves the contract survives scores and thresholds. Deleted once [AI Integration](ai-integration-spec.md) ships a real one.
- [x] Unit tests: ordering/short-circuit, cache hit/miss (including the no-`VersionId` skip path), notification publishing, variant addressing (`HasProperty` vs empty value), config schema field/validation inference, the base class's default-config fallback when no config is stored. **Not yet covered:** `Warn` status, and a check throwing → `NotEvaluated(Error)` — real gaps, not claimed as done.
- [x] Build against a real `IContent` in an integration test, not just `InMemoryContentCheckSnapshot` — `Umbraco.Test.ContentChecks.Integration`'s `ContentCheckResolutionServiceIntegrationTests` (2026-08-08) runs `IContentCheckResolutionService.ResolveAsync` against a real, EF-backed `IContentType`/`IContent` via `Umbraco.Cms.Tests.Integration`'s SQLite harness, which exercises `ContentCheckSnapshot` (the real adapter) with genuine `Key`/`VersionId`/`HasProperty` values from a saved `IContent` — not just its mocked stand-in.

**Acceptance:** a test runs a mixed check policy over a real content item and gets a report where the `Metered` check is short-circuited by a failing `Blocking` `Free` check. **Met** — `ContentCheckRunnerTests.RunAsync_BlockingFreeFailure_ShortCircuitsMeteredRules`, against `InMemoryContentCheckSnapshot` rather than a real `IContent` (the gap noted above).

**Review gate: not yet done.** Re-read the contract against all 14 findings in the [Overview](overview.md) before moving to Phase 2. This is the one-way door — the next concrete step, not yet started.
