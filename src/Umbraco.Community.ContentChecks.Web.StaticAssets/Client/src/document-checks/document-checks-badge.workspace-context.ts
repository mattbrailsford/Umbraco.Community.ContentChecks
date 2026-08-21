import { UmbContextBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UMB_DOCUMENT_WORKSPACE_CONTEXT } from "@umbraco-cms/backoffice/document";
import type { UmbHintController } from "@umbraco-cms/backoffice/hint";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { CheckResultsService } from "../api/sdk.gen.js";
import type { ContentCheckAggregateReportEntry } from "../api/types.gen.js";
import {
    CONTENT_CHECKS_DOCUMENT_CHECKS_BADGE_WORKSPACE_CONTEXT_ALIAS,
    CONTENT_CHECKS_DOCUMENT_CHECKS_WORKSPACE_VIEW_ALIAS,
} from "./constants.js";

const FAILURE_HINT_UNIQUE = "content-checks-failures";

function countFailures(entries: ContentCheckAggregateReportEntry[]) {
    let mandatoryFailed = 0;
    let recommendedFailed = 0;
    for (const { entry } of entries) {
        if (entry.result.status !== "Fail" && entry.result.status !== "Warn") continue;
        if (entry.requirementLevel === "Mandatory") mandatoryFailed++;
        else recommendedFailed++;
    }
    return { mandatoryFailed, recommendedFailed };
}

/**
 * Puts the same failure-count badge on the "Checks" tab that `cc-document-checks-workspace-view`
 * puts on itself once opened -- but from a `workspaceContext`, which (unlike a `workspaceView`
 * element) is alive for the document workspace's whole lifetime regardless of which tab is active.
 * Without this, the badge only ever appeared after a user had visited the Checks tab at least once
 * in the current session, since that's the only thing that triggered the check-results fetch.
 */
export class CcDocumentChecksBadgeWorkspaceContext extends UmbContextBase {
    constructor(host: UmbControllerHost) {
        super(host, CONTENT_CHECKS_DOCUMENT_CHECKS_BADGE_WORKSPACE_CONTEXT_ALIAS);

        this.consumeContext(UMB_DOCUMENT_WORKSPACE_CONTEXT, (context) => {
            this.observe(
                context?.unique,
                (unique) => {
                    if (unique) {
                        this.#load(unique, context!.view.hints);
                    } else {
                        context?.view.hints.removeOne(FAILURE_HINT_UNIQUE);
                    }
                },
                "_observeUnique",
            );
        });
    }

    async #load(contentKey: string, hints: UmbHintController) {
        const { data: report } = await tryExecute(this, CheckResultsService.getCheckResultsByContentKey({ path: { contentKey } }));

        hints.removeOne(FAILURE_HINT_UNIQUE);
        if (!report) return;

        const { mandatoryFailed, recommendedFailed } = countFailures(report.entries);
        const failed = mandatoryFailed + recommendedFailed;
        if (failed === 0) return;

        hints.addOne({
            unique: FAILURE_HINT_UNIQUE,
            path: [CONTENT_CHECKS_DOCUMENT_CHECKS_WORKSPACE_VIEW_ALIAS],
            text: String(failed),
            color: mandatoryFailed > 0 ? "invalid" : "warning",
        });
    }
}

export default CcDocumentChecksBadgeWorkspaceContext;
