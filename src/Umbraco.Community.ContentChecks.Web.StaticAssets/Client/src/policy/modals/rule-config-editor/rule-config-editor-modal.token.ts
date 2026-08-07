import { UmbModalToken } from "@umbraco-cms/backoffice/modal";
import type { CheckTypeResponseModel } from "../../../api/types.gen.js";
import type { ContentChecksPolicyRuleModel } from "../../types.js";

export interface ContentChecksRuleConfigEditorModalData {
    checkType: CheckTypeResponseModel;
    existingRule?: ContentChecksPolicyRuleModel;
}

export interface ContentChecksRuleConfigEditorModalValue {
    rule: ContentChecksPolicyRuleModel;
}

export const CONTENT_CHECKS_RULE_CONFIG_EDITOR_MODAL = new UmbModalToken<
    ContentChecksRuleConfigEditorModalData,
    ContentChecksRuleConfigEditorModalValue
>("ContentChecks.Modal.RuleConfigEditor", {
    modal: {
        type: "sidebar",
        size: "medium",
    },
});
