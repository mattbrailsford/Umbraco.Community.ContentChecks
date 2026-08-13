import { css, customElement, html, state } from "@umbraco-cms/backoffice/external/lit";
import { HubConnectionBuilder, type HubConnection } from "@umbraco-cms/backoffice/external/signalr";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { UMB_AUTH_CONTEXT } from "@umbraco-cms/backoffice/auth";
import { UMB_ENTITY_CONTEXT } from "@umbraco-cms/backoffice/entity";
import { UMB_SERVER_CONTEXT } from "@umbraco-cms/backoffice/server";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { ApplicableChecksService, CheckResultsService, EvaluationService } from "../api/sdk.gen.js";
import type {
    ApplicableCheckTargetResponseModel,
    ApplicableRuleResponseModel,
    ContentCheckAggregateReport,
    ContentCheckReportEntry,
    EvaluationItemResponseModel,
    EvaluationRunResponseModel,
} from "../api/types.gen.js";

/**
 * Must match `Constants.ManagementApi.RunHubPath` in `Umbraco.Community.ContentChecks.Web` --
 * where `ContentCheckRunHub` is mapped to.
 */
const RUN_HUB_PATH = "/umbraco/content-checks/run-hub";

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

    @state()
    private _runError?: string;

    /** Keyed by `${JSON.stringify(path)}::${ruleKey}` so a result can be matched back to its pending row. */
    @state()
    private _resultsByKey: Map<string, ContentCheckReportEntry> = new Map();

    #connection?: HubConnection;

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
    }

    override disconnectedCallback() {
        super.disconnectedCallback();
        this.#connection?.stop();
    }

    async #loadApplicableChecks() {
        if (!this._contentKey) return;
        this._isLoading = true;
        this._resultsByKey = new Map();

        const [{ data: targets }, { data: report }] = await Promise.all([
            tryExecute(this, ApplicableChecksService.getApplicableChecksByContentKey({ path: { contentKey: this._contentKey } })),
            tryExecute(this, CheckResultsService.getCheckResultsByContentKey({ path: { contentKey: this._contentKey } })),
        ]);

        this._targets = targets ?? [];
        if (report) {
            this.#applyReport(report);
        }

        this._isLoading = false;
    }

    /**
     * Keyed by `ruleKey`, not `checkAlias` -- two rules can run the same check at the same
     * location with different config (e.g. two "required property" rules checking different
     * properties), and `checkAlias` alone can't tell them apart.
     */
    #resultKey(path: unknown, ruleKey: string) {
        return `${JSON.stringify(path)}::${ruleKey}`;
    }

    async #onRun() {
        if (!this._contentKey || this._isRunning) return;

        this._isRunning = true;
        this._runError = undefined;
        this._resultsByKey = new Map();

        const { data: startData, error: startError } = await tryExecute(
            this,
            EvaluationService.postEvaluation({ body: { contentKeys: [this._contentKey] } }),
        );

        if (startError || !startData) {
            this._runError = "Could not start the run.";
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
            this._runError = "Could not watch the run.";
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
            this._runError = "Could not watch the run.";
            await this.#refreshResults();
            this.#stopWatching();
        }
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
            this._runError = item.error;
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
    }

    #renderResult(path: unknown, ruleKey: string) {
        const result = this._resultsByKey.get(this.#resultKey(path, ruleKey));
        if (!result) {
            return this._isRunning
                ? html`<uui-loader-bar></uui-loader-bar>`
                : html`<span class="pending">Not run yet</span>`;
        }

        const color =
            result.result.status === "Fail"
                ? "danger"
                : result.result.status === "Warn"
                  ? "warning"
                  : result.result.status === "Pass"
                    ? "positive"
                    : "default";

        return html`
            <uui-tag color=${color}>${result.result.status}</uui-tag>
            <span class="message">${result.result.message}</span>
        `;
    }

    #renderRule(target: ApplicableCheckTargetResponseModel, rule: ApplicableRuleResponseModel) {
        return html`
            <div class="rule-row">
                <span class="check-name">${rule.checkName}</span>
                <uui-tag look="outline">${rule.requirementLevel}</uui-tag>
                <div class="result">${this.#renderResult(target.path, rule.ruleKey)}</div>
            </div>
        `;
    }

    #renderTarget(target: ApplicableCheckTargetResponseModel) {
        const label =
            target.path.length === 0
                ? target.contentTypeAlias
                : `${target.contentTypeAlias} (via ${target.path.map((p) => p.propertyAlias).join(" / ")})`;

        return html`
            <uui-box .headline=${label}>
                ${target.policyMatches.map(
                    (match) => html`
                        <div class="policy-match">
                            <div class="policy-header">
                                <strong>${match.policyName}</strong>
                                <uui-tag look="outline">${match.provenance}</uui-tag>
                            </div>
                            ${match.rules.map((rule) => this.#renderRule(target, rule))}
                        </div>
                    `,
                )}
            </uui-box>
        `;
    }

    override render() {
        return html`
            <div id="toolbar">
                <uui-button
                    label="Run checks"
                    look="primary"
                    ?disabled=${this._isRunning || this._targets.length === 0}
                    @click=${this.#onRun}
                >
                    ${this._isRunning ? html`<uui-loader-circle></uui-loader-circle>` : "Run checks"}
                </uui-button>
                ${this._runError ? html`<uui-tag color="danger">${this._runError}</uui-tag>` : ""}
            </div>

            ${this._isLoading
                ? html`<uui-loader-bar></uui-loader-bar>`
                : this._targets.length === 0
                  ? html`<p class="placeholder-text">No check policies apply to this content.</p>`
                  : this._targets.map((target) => this.#renderTarget(target))}
        `;
    }

    static override styles = [
        UmbTextStyles,
        css`
            :host {
                display: block;
                padding: var(--uui-size-layout-1);
            }
            #toolbar {
                display: flex;
                align-items: center;
                gap: var(--uui-size-space-4);
                margin-bottom: var(--uui-size-layout-1);
            }
            uui-box {
                margin-bottom: var(--uui-size-space-4);
            }
            .policy-match {
                margin-bottom: var(--uui-size-space-4);
            }
            .policy-header {
                display: flex;
                align-items: center;
                gap: var(--uui-size-space-3);
                margin-bottom: var(--uui-size-space-2);
            }
            .rule-row {
                display: flex;
                align-items: center;
                gap: var(--uui-size-space-3);
                padding: var(--uui-size-space-2) 0;
                border-top: 1px solid var(--uui-color-divider);
            }
            .check-name {
                min-width: 200px;
            }
            .result {
                display: flex;
                align-items: center;
                gap: var(--uui-size-space-3);
            }
            .pending {
                color: var(--uui-color-text-alt);
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
