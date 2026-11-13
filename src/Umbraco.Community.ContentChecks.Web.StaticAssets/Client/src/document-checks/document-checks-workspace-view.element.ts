import { css, customElement, html, state } from "@umbraco-cms/backoffice/external/lit";
import { HubConnectionBuilder, type HubConnection } from "@umbraco-cms/backoffice/external/signalr";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { UMB_AUTH_CONTEXT } from "@umbraco-cms/backoffice/auth";
import { UMB_ENTITY_CONTEXT } from "@umbraco-cms/backoffice/entity";
import { UMB_HINT_CONTEXT, type UmbHintController } from "@umbraco-cms/backoffice/hint";
import { UMB_NOTIFICATION_CONTEXT } from "@umbraco-cms/backoffice/notification";
import { UMB_SERVER_CONTEXT } from "@umbraco-cms/backoffice/server";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { ApplicableChecksService, CheckResultsService, EvaluationService, PoliciesService } from "../api/sdk.gen.js";
import type {
    ApplicableCheckTargetResponseModel,
    ApplicableRuleResponseModel,
    ApplicablePolicyMatchResponseModel,
    ContentCheckAggregateReport,
    ContentCheckReportEntry,
    ContentCheckStatus,
    EvaluationItemResponseModel,
    EvaluationRunResponseModel,
    RequirementLevel,
} from "../api/types.gen.js";

/**
 * Must match `Constants.ManagementApi.RunHubPath` in `Umbraco.Community.ContentChecks.Web` --
 * where `ContentCheckRunHub` is mapped to.
 */
const RUN_HUB_PATH = "/umbraco/content-checks/run-hub";

/** Stable id for the tab-badge hint, so a re-computed count replaces the old one rather than stacking. */
const FAILURE_HINT_UNIQUE = "content-checks-failures";

const RELATIVE_TIME_FORMATTER = new Intl.RelativeTimeFormat(undefined, { numeric: "auto" });

/**
 * How a rule's outcome rolls up for the summary bar and its row's dot colour. A Fail or Warn
 * splits by requirement level rather than landing in one shared "failed" bucket -- a Mandatory
 * failure is the one that actually blocks something, so it's the one that should read as urgent
 * (red); a Recommended/Optional one still needs fixing, just not as loudly (orange).
 */
type StatusBucket = "mandatoryFailed" | "recommendedFailed" | "passed" | "notEvaluated";

type CheckSummary = {
    mandatoryFailed: number;
    recommendedFailed: number;
    passed: number;
    notEvaluated: number;
    total: number;
};

function bucketOf(status: ContentCheckStatus | undefined, requirementLevel: RequirementLevel): StatusBucket {
    if (status === "Fail" || status === "Warn") return requirementLevel === "Mandatory" ? "mandatoryFailed" : "recommendedFailed";
    if (status === "Pass") return "passed";
    return "notEvaluated";
}

function pluralize(count: number, singular: string, plural = `${singular}s`) {
    return count === 1 ? singular : plural;
}

/**
 * A rule's config is a free-form JSON blob (`PolicyRuleModel.config`), shaped per check type. Only
 * checks built around a single target property (e.g. `RequiredPropertyCheck`) expose a
 * `propertyAlias` key -- other check kinds simply won't have one, which is fine, the meta line just
 * omits it.
 */
function extractPropertyAlias(config: unknown): string | undefined {
    if (config && typeof config === "object" && "propertyAlias" in config) {
        const value = (config as Record<string, unknown>).propertyAlias;
        return typeof value === "string" ? value : undefined;
    }
    return undefined;
}

/**
 * One panel, not two: renders resolution's own per-item Applicable Checks as pending rows, then
 * fills each row in place -- either from the last completed run's persisted results (loaded on
 * open, so a previous run's outcome shows without needing to re-run) or as a live run reports new
 * ones. No separate "results" view to reconcile against the pending one.
 */
@customElement("cc-document-checks-workspace-view")
export class CcDocumentChecksWorkspaceViewElement extends UmbLitElement {
    @state()
    private _contentKey?: string;

    @state()
    private _targets: ApplicableCheckTargetResponseModel[] = [];

    @state()
    private _isLoading = false;

    @state()
    private _isRunning = false;

    /** Keyed by `${JSON.stringify(path)}::${ruleKey}` so a result can be matched back to its pending row. */
    @state()
    private _resultsByKey: Map<string, ContentCheckReportEntry> = new Map();

    /** Set once a report (persisted or live) with at least one entry has been applied. */
    @state()
    private _ranAtUtc?: string;

    /** Keyed by `ruleKey`, sourced from each rule's policy config, e.g. `{ propertyAlias: "metaDescription" }`. */
    @state()
    private _propertyAliasByRuleKey: Map<string, string> = new Map();

    /** Ticks periodically so the "Last run X ago" label stays fresh without user interaction. */
    @state()
    private _now = Date.now();

    #connection?: HubConnection;
    #clockTimer?: ReturnType<typeof setInterval>;
    #hintContext?: UmbHintController;

    constructor() {
        super();
        this.consumeContext(UMB_ENTITY_CONTEXT, (context) => {
            this.observe(
                context?.unique,
                (unique) => {
                    if (unique && unique !== this._contentKey) {
                        this._contentKey = unique;
                        this.#loadApplicableChecks();
                    }
                },
                "_observeUnique",
            );
        });
        this.consumeContext(UMB_HINT_CONTEXT, (context) => {
            this.#hintContext = context;
            this.#updateFailureHint();
        });
    }

    override connectedCallback() {
        super.connectedCallback();
        this.#clockTimer = setInterval(() => {
            this._now = Date.now();
        }, 30_000);
    }

    override disconnectedCallback() {
        super.disconnectedCallback();
        this.#connection?.stop();
        clearInterval(this.#clockTimer);
    }

    async #loadApplicableChecks() {
        if (!this._contentKey) return;
        this._isLoading = true;
        this._resultsByKey = new Map();
        this._ranAtUtc = undefined;
        // Clear immediately rather than waiting on the fetch below -- otherwise a badge left over
        // from whatever content item was open before this one stays visible while loading.
        this.#hintContext?.removeOne(FAILURE_HINT_UNIQUE);

        const [{ data: targets }, { data: report }] = await Promise.all([
            tryExecute(this, ApplicableChecksService.getApplicableChecksByContentKey({ path: { contentKey: this._contentKey } })),
            tryExecute(this, CheckResultsService.getCheckResultsByContentKey({ path: { contentKey: this._contentKey } })),
        ]);

        this._targets = targets ?? [];
        if (report) {
            this.#applyReport(report);
        }

        await this.#loadPropertyAliases(this._targets);

        this._isLoading = false;
    }

    async #loadPropertyAliases(targets: ApplicableCheckTargetResponseModel[]) {
        const policyKeys = [...new Set(targets.flatMap((target) => target.policyMatches.map((match) => match.policyKey)))];

        const policies = await Promise.all(
            policyKeys.map((key) => tryExecute(this, PoliciesService.getPoliciesByKey({ path: { key } }))),
        );

        const next = new Map<string, string>();
        for (const { data: policy } of policies) {
            if (!policy) continue;
            for (const rule of policy.rules) {
                const propertyAlias = extractPropertyAlias(rule.config);
                if (propertyAlias) next.set(rule.key, propertyAlias);
            }
        }
        this._propertyAliasByRuleKey = next;
    }

    /**
     * Keyed by `ruleKey`, not `checkAlias` -- two rules can run the same check at the same
     * location with different config (e.g. two "required property" rules checking different
     * properties), and `checkAlias` alone can't tell them apart.
     */
    #resultKey(path: unknown, ruleKey: string) {
        return `${JSON.stringify(path)}::${ruleKey}`;
    }

    #statusOf(target: ApplicableCheckTargetResponseModel, rule: ApplicableRuleResponseModel): ContentCheckStatus | undefined {
        return this._resultsByKey.get(this.#resultKey(target.path, rule.ruleKey))?.result.status;
    }

    async #onRun() {
        if (!this._contentKey || this._isRunning) return;

        this._isRunning = true;
        this._resultsByKey = new Map();

        const { data: startData, error: startError } = await tryExecute(
            this,
            EvaluationService.postEvaluation({ body: { contentKeys: [this._contentKey] } }),
        );

        if (startError || !startData) {
            await this.#notifyError("Could not start the run.");
            this._isRunning = false;
            return;
        }

        await this.#watchRun(startData.runId);
    }

    /**
     * Watches a run over the backoffice's own SignalR hub, joining the run's group so the server
     * pushes progress as it happens -- no polling. `JoinRun` also replays whatever's already
     * known about the run as soon as it's joined, so a client that connects after some (or all)
     * items have already finished -- a fast run, or a slow page load -- still sees their state,
     * not just what happens from the moment it joins.
     */
    async #watchRun(runId: string) {
        const [authContext, serverContext] = await Promise.all([
            this.getContext(UMB_AUTH_CONTEXT),
            this.getContext(UMB_SERVER_CONTEXT),
        ]);

        if (!authContext || !serverContext) {
            await this.#notifyError("Could not watch the run.");
            this._isRunning = false;
            return;
        }

        this.#connection = new HubConnectionBuilder()
            .withUrl(`${serverContext.getServerUrl()}${RUN_HUB_PATH}`, {
                accessTokenFactory: () => authContext.getLatestToken(),
            })
            .withAutomaticReconnect()
            .build();

        this.#connection.on("progress", (item: EvaluationItemResponseModel) => this.#applyItemResult(item));
        this.#connection.on("complete", (run: EvaluationRunResponseModel) => {
            run.items.forEach((item) => this.#applyItemResult(item));
            this.#stopWatching();
        });

        try {
            await this.#connection.start();
            await this.#connection.invoke("JoinRun", runId);
        } catch {
            // The hub connection itself failed (e.g. a proxy blocking the WebSocket upgrade) --
            // fall back to a single read of whatever's been persisted so far rather than leaving
            // every row stuck on "Not run yet".
            await this.#notifyError("Could not watch the run.");
            await this.#refreshResults();
            this.#stopWatching();
        }
    }

    async #notifyError(message: string) {
        const notificationContext = await this.getContext(UMB_NOTIFICATION_CONTEXT);
        notificationContext?.peek("danger", { data: { headline: "Content checks", message } });
    }

    #stopWatching() {
        this.#connection?.stop();
        this.#connection = undefined;
        this._isRunning = false;
    }

    async #refreshResults() {
        if (!this._contentKey) return;
        const { data: report } = await tryExecute(
            this,
            CheckResultsService.getCheckResultsByContentKey({ path: { contentKey: this._contentKey } }),
        );
        if (report) {
            this.#applyReport(report);
        }
    }

    #applyItemResult(item: EvaluationItemResponseModel) {
        if (item.error) {
            this.#notifyError(item.error);
            return;
        }
        if (!item.report) return;

        this.#applyReport(item.report);
    }

    #applyReport(report: ContentCheckAggregateReport) {
        const next = new Map(this._resultsByKey);
        for (const entry of report.entries) {
            next.set(this.#resultKey(entry.path, entry.entry.ruleKey), entry.entry);
        }
        this._resultsByKey = next;
        if (report.ranAtUtc) {
            this._ranAtUtc = report.ranAtUtc;
        }
        this.#updateFailureHint();
    }

    /**
     * Mirrors the summary bar's own failure count onto this view's tab in the document workspace's
     * tab strip -- a Mandatory failure renders as Umbraco's own "invalid" (red, attention-pulsing)
     * badge colour, same as a property validation error; a Recommended/Optional-only failure is a
     * plain warning badge instead, since nothing about it blocks anything.
     */
    #updateFailureHint() {
        if (!this.#hintContext) return;
        this.#hintContext.removeOne(FAILURE_HINT_UNIQUE);

        if (this._ranAtUtc === undefined) return;

        const summary = this.#computeSummary();
        const failed = summary.mandatoryFailed + summary.recommendedFailed;
        if (failed === 0) return;

        this.#hintContext.addOne({
            unique: FAILURE_HINT_UNIQUE,
            text: String(failed),
            color: summary.mandatoryFailed > 0 ? "invalid" : "warning",
        });
    }

    #formatLastRun(iso: string) {
        const diffSeconds = Math.round((new Date(iso).getTime() - this._now) / 1000);
        const diffMinutes = Math.round(diffSeconds / 60);
        if (Math.abs(diffMinutes) < 1) return RELATIVE_TIME_FORMATTER.format(diffSeconds, "second");
        const diffHours = Math.round(diffMinutes / 60);
        if (Math.abs(diffHours) < 1) return RELATIVE_TIME_FORMATTER.format(diffMinutes, "minute");
        const diffDays = Math.round(diffHours / 24);
        if (Math.abs(diffDays) < 1) return RELATIVE_TIME_FORMATTER.format(diffHours, "hour");
        return RELATIVE_TIME_FORMATTER.format(diffDays, "day");
    }

    #computeSummary(): CheckSummary {
        let mandatoryFailed = 0;
        let recommendedFailed = 0;
        let passed = 0;
        let notEvaluated = 0;

        for (const target of this._targets) {
            for (const match of target.policyMatches) {
                for (const rule of match.rules) {
                    const bucket = bucketOf(this.#statusOf(target, rule), rule.requirementLevel);
                    if (bucket === "mandatoryFailed") mandatoryFailed++;
                    else if (bucket === "recommendedFailed") recommendedFailed++;
                    else if (bucket === "passed") passed++;
                    else notEvaluated++;
                }
            }
        }

        return { mandatoryFailed, recommendedFailed, passed, notEvaluated, total: mandatoryFailed + recommendedFailed + passed + notEvaluated };
    }

    #renderProgressBar(summary: CheckSummary) {
        const pct = (count: number) => (summary.total === 0 ? 0 : (count / summary.total) * 100);

        return html`
            <div class="progress-bar">
                <div class="segment mandatoryFailed" style="width:${pct(summary.mandatoryFailed)}%"></div>
                <div class="segment recommendedFailed" style="width:${pct(summary.recommendedFailed)}%"></div>
                <div class="segment passed" style="width:${pct(summary.passed)}%"></div>
                <div class="segment notEvaluated" style="width:${pct(summary.notEvaluated)}%"></div>
            </div>
            <div class="legend">
                <span class="legend-item"><span class="dot mandatoryFailed"></span>${summary.mandatoryFailed} mandatory failed</span>
                <span class="legend-item"><span class="dot recommendedFailed"></span>${summary.recommendedFailed} recommended failed</span>
                <span class="legend-item"><span class="dot passed"></span>${summary.passed} passed</span>
                <span class="legend-item"><span class="dot notEvaluated"></span>${summary.notEvaluated} not evaluated</span>
            </div>
        `;
    }

    #renderSummary() {
        const policyInstanceCount = this._targets.flatMap((t) => t.policyMatches).length;
        const totalRuleCount = this._targets.flatMap((t) => t.policyMatches).flatMap((m) => m.rules).length;
        const hasRun = this._ranAtUtc !== undefined;

        let headline: string;
        let subtext: string;

        if (!hasRun) {
            headline = "Not run yet";
            subtext = `${policyInstanceCount} ${pluralize(policyInstanceCount, "policy instance", "policy instances")} ${
                policyInstanceCount === 1 ? "applies" : "apply"
            } to this document. ${totalRuleCount} ${pluralize(totalRuleCount, "check")} will run.`;
        } else {
            const summary = this.#computeSummary();
            const failed = summary.mandatoryFailed + summary.recommendedFailed;
            if (failed > 0) {
                headline = `${failed} ${pluralize(failed, "check")} failed`;
                subtext =
                    summary.mandatoryFailed > 0 && summary.recommendedFailed > 0
                        ? `${summary.mandatoryFailed} mandatory, ${summary.recommendedFailed} recommended.`
                        : summary.mandatoryFailed > 0
                          ? `${summary.mandatoryFailed} mandatory ${pluralize(summary.mandatoryFailed, "check")} failed.`
                          : `${summary.recommendedFailed} recommended ${pluralize(summary.recommendedFailed, "check")} failed.`;
            } else {
                headline = "All checks passed";
                subtext =
                    summary.notEvaluated > 0
                        ? `${summary.notEvaluated} ${pluralize(summary.notEvaluated, "check")} not evaluated.`
                        : "Nothing to fix.";
            }
        }

        return html`
            <uui-box class="summary-card">
                <div class="summary-top">
                    <div>
                        <h3>${headline}</h3>
                        <p class="summary-sub">${subtext}</p>
                    </div>
                    <div class="summary-actions">
                        ${hasRun ? html`<span class="last-run">Last run ${this.#formatLastRun(this._ranAtUtc!)}</span>` : ""}
                        <uui-button
                            label=${hasRun ? "Run again" : "Run checks"}
                            look="primary"
                            ?disabled=${this._isRunning || totalRuleCount === 0}
                            @click=${this.#onRun}
                        >
                            ${this._isRunning ? html`<uui-loader-circle></uui-loader-circle>` : hasRun ? "Run again" : "Run checks"}
                        </uui-button>
                    </div>
                </div>
                ${hasRun ? this.#renderProgressBar(this.#computeSummary()) : ""}
            </uui-box>
        `;
    }

    /**
     * A Warn always renders as its own "Warning" pill, regardless of requirement level -- it's a
     * different concept from a Fail (a check that ran and flagged a concern, not a hard failure).
     * A Fail keeps the word "Failed" either way; only its colour drops from red to orange for a
     * Recommended/Optional rule, since that's the one that isn't actually blocking anything.
     */
    #renderRuleStatus(status: ContentCheckStatus | undefined, requirementLevel: RequirementLevel) {
        if (!status) {
            return this._isRunning
                ? html`<uui-loader-circle></uui-loader-circle>`
                : html`<span class="status-text muted">Not evaluated</span>`;
        }
        if (status === "Fail") return html`<uui-tag color=${requirementLevel === "Mandatory" ? "danger" : "warning"}>Failed</uui-tag>`;
        if (status === "Warn") return html`<uui-tag color="warning">Warning</uui-tag>`;
        if (status === "Pass") return html`<span class="status-text positive">Passed</span>`;
        return html`<span class="status-text muted">Not evaluated</span>`;
    }

    #renderRule(target: ApplicableCheckTargetResponseModel, rule: ApplicableRuleResponseModel) {
        const entry = this._resultsByKey.get(this.#resultKey(target.path, rule.ruleKey));
        const status = entry?.result.status;
        const bucket = bucketOf(status, rule.requirementLevel);

        // For a nested target, name the actual property/element chain reached to find it (e.g.
        // "heroBlock #2 / seoSettings") rather than just its own type -- the type alone doesn't
        // say where on the page it is, which matters most for a Cascading match sitting several
        // blocks deep.
        const scope =
            target.path.length === 0
                ? "On this document"
                : `On ${target.path.map((step) => `${step.propertyAlias} #${step.index}`).join(" / ")}`;
        const propertyAlias = this._propertyAliasByRuleKey.get(rule.ruleKey);
        const meta = [rule.requirementLevel, propertyAlias].filter(Boolean).join(" · ");

        const subtitle = entry ? entry.result.message : this._isRunning ? "Running…" : "Not evaluated yet";

        return html`
            <div class="rule-row">
                <span class="status-dot ${bucket}"></span>
                <div class="rule-body">
                    <div class="rule-title">${rule.ruleName || rule.checkName}</div>
                    <div class="rule-subtitle ${entry ? "" : "pending"}">${subtitle}</div>
                    <div class="rule-meta">${meta} — ${scope}</div>
                </div>
                <div class="rule-status">${this.#renderRuleStatus(status, rule.requirementLevel)}</div>
            </div>
        `;
    }

    #renderPolicyCount(target: ApplicableCheckTargetResponseModel, match: ApplicablePolicyMatchResponseModel) {
        if (this._ranAtUtc === undefined) {
            return html`<span class="policy-count muted">${match.rules.length} ${pluralize(match.rules.length, "check")}</span>`;
        }

        const buckets = match.rules.map((rule) => bucketOf(this.#statusOf(target, rule), rule.requirementLevel));
        const mandatoryFailed = buckets.filter((b) => b === "mandatoryFailed").length;
        const recommendedFailed = buckets.filter((b) => b === "recommendedFailed").length;
        const failed = mandatoryFailed + recommendedFailed;
        if (failed > 0) {
            return html`<span class="policy-count ${mandatoryFailed > 0 ? "danger" : "warning"}">${failed} failed</span>`;
        }

        const notEvaluated = buckets.filter((b) => b === "notEvaluated").length;
        if (notEvaluated === match.rules.length) {
            return html`<span class="policy-count muted">${match.rules.length} ${pluralize(match.rules.length, "check")}</span>`;
        }

        return html`<span class="policy-count positive">All passed</span>`;
    }

    #renderPolicyCard(target: ApplicableCheckTargetResponseModel, match: ApplicablePolicyMatchResponseModel) {
        // The content type the POLICY is configured against (e.g. "Seo Base"), not the type of
        // whatever's actually being checked -- a Composition/Cascading match applies here
        // precisely because this content's own type composes or inherits that type, so naming the
        // resolved target instead would just repeat this document's own type. Falls back to the
        // resolved target's type if the policy's own type has since been deleted. The actual
        // nested location (for a Cascading match) is shown per-row instead, in the meta line.
        const scopeLabel = match.contentTypeName ?? target.contentTypeAlias;
        const verb = match.provenance === "Direct" ? "on" : "via";

        return html`
            <uui-box class="policy-card" .headline=${match.policyName}>
                <uui-tag slot="header" look="outline">${match.provenance}</uui-tag>
                <span slot="header" class="policy-scope">${verb} ${scopeLabel}</span>
                <span slot="header-actions">${this.#renderPolicyCount(target, match)}</span>
                ${match.rules.map((rule) => this.#renderRule(target, rule))}
            </uui-box>
        `;
    }

    override render() {
        return html`
            ${this._isLoading
                ? html`<uui-loader-bar></uui-loader-bar>`
                : this._targets.length === 0
                  ? html`<p class="placeholder-text">No check policies apply to this content.</p>`
                  : html`
                        ${this.#renderSummary()}
                        ${this._targets.flatMap((target) =>
                            target.policyMatches.map((match) => this.#renderPolicyCard(target, match)),
                        )}
                    `}
        `;
    }

    static override styles = [
        UmbTextStyles,
        css`
            :host {
                display: block;
                padding: var(--uui-size-layout-1);
            }
            uui-box {
                margin-bottom: var(--uui-size-space-4);
            }
            .summary-top {
                display: flex;
                align-items: flex-start;
                justify-content: space-between;
                gap: var(--uui-size-space-5);
            }
            .summary-top h3 {
                margin: 0;
            }
            .summary-sub {
                margin: var(--uui-size-space-2) 0 0;
                color: var(--uui-color-text-alt);
            }
            .summary-actions {
                display: flex;
                align-items: center;
                gap: var(--uui-size-space-4);
                flex-shrink: 0;
            }
            .last-run {
                color: var(--uui-color-text-alt);
                font-size: 0.85rem;
                white-space: nowrap;
            }
            .progress-bar {
                display: flex;
                height: 8px;
                border-radius: 4px;
                overflow: hidden;
                margin-top: var(--uui-size-space-6);
                background: var(--uui-color-disabled, #e0e0e0);
            }
            .segment {
                height: 100%;
            }
            .segment.mandatoryFailed,
            .dot.mandatoryFailed,
            .status-dot.mandatoryFailed {
                background: var(--uui-color-danger, #ee2b53);
            }
            .segment.recommendedFailed,
            .dot.recommendedFailed,
            .status-dot.recommendedFailed {
                background: var(--uui-color-warning, #f4a52a);
            }
            .segment.passed,
            .dot.passed,
            .status-dot.passed {
                background: var(--uui-color-positive, #43c17a);
            }
            .segment.notEvaluated,
            .dot.notEvaluated,
            .status-dot.notEvaluated {
                background: var(--uui-color-disabled, #cacaca);
            }
            .dot.notEvaluated,
            .status-dot.notEvaluated {
                border: 1px solid var(--uui-color-disabled-standalone, #b3b3b3);
            }
            .legend {
                display: flex;
                flex-wrap: wrap;
                gap: var(--uui-size-space-2) var(--uui-size-space-5);
                margin-top: var(--uui-size-space-3);
                font-size: 0.85rem;
                color: var(--uui-color-text-alt);
            }
            .legend-item {
                display: flex;
                align-items: center;
                gap: var(--uui-size-space-2);
            }
            .dot {
                box-sizing: border-box;
                width: 8px;
                height: 8px;
                border-radius: 50%;
                display: inline-block;
            }
            .policy-scope {
                color: var(--uui-color-text-alt);
                font-size: 0.9rem;
            }
            .policy-count {
                font-size: 0.9rem;
            }
            .policy-count.danger {
                color: var(--uui-color-danger-standalone, #d42054);
                font-weight: 600;
            }
            .policy-count.warning {
                color: var(--uui-color-warning-standalone, #a35200);
                font-weight: 600;
            }
            .policy-count.positive {
                color: var(--uui-color-positive-standalone, #3a8547);
            }
            .policy-count.muted {
                color: var(--uui-color-text-alt);
            }
            .rule-row {
                display: flex;
                align-items: flex-start;
                gap: var(--uui-size-space-4);
                padding: var(--uui-size-space-4) 0;
            }
            .rule-row + .rule-row {
                border-top: 1px solid var(--uui-color-divider);
            }
            .rule-row:last-child {
                padding-bottom: 0;
            }
            .rule-row:first-of-type {
                padding-top: 0;
            }
            .status-dot {
                box-sizing: border-box;
                width: 8px;
                height: 8px;
                border-radius: 50%;
                flex-shrink: 0;
                margin-top: 6px;
            }
            .rule-body {
                flex: 1;
                min-width: 0;
            }
            .rule-title {
                font-weight: 600;
            }
            .rule-subtitle {
                color: var(--uui-color-text-alt);
                font-size: 0.9rem;
                margin-top: 2px;
            }
            .rule-subtitle.pending {
                opacity: 0.65;
            }
            .rule-meta {
                color: var(--uui-color-text-alt);
                font-size: 0.8rem;
                margin-top: var(--uui-size-space-2);
                opacity: 0.6;
            }
            .rule-status {
                flex-shrink: 0;
                padding-top: 2px;
            }
            .status-text.muted {
                color: var(--uui-color-text-alt);
            }
            .status-text.positive {
                color: var(--uui-color-positive-standalone, #3a8547);
            }
            .placeholder-text {
                color: var(--uui-color-text-alt);
                font-style: italic;
            }
        `,
    ];
}

export default CcDocumentChecksWorkspaceViewElement;

declare global {
    interface HTMLElementTagNameMap {
        "cc-document-checks-workspace-view": CcDocumentChecksWorkspaceViewElement;
    }
}
