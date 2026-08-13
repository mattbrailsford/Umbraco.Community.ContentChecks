import { UmbModalToken } from "@umbraco-cms/backoffice/modal";
import type { ContentChecksGuardRuleModel } from "../../types.js";

export interface ContentChecksRuleEditorModalData {
    existingRule?: ContentChecksGuardRuleModel;
}

export interface ContentChecksRuleEditorModalValue {
    rule: ContentChecksGuardRuleModel;
}

export const CONTENT_CHECKS_RULE_EDITOR_MODAL = new UmbModalToken<
    ContentChecksRuleEditorModalData,
    ContentChecksRuleEditorModalValue
>("ContentChecks.Modal.RuleEditor", {
    modal: {
        type: "sidebar",
        size: "small",
    },
});
