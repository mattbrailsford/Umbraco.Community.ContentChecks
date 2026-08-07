import { UmbModalToken } from "@umbraco-cms/backoffice/modal";
import type { CheckTypeResponseModel } from "../../../api/types.gen.js";
import type { ContentChecksPolicyRuleModel } from "../../types.js";

export interface ContentChecksRuleTypePickerModalData {
    checkTypes: CheckTypeResponseModel[];
    headline?: string;
}

export interface ContentChecksRuleTypePickerModalValue {
    checkType: CheckTypeResponseModel;
    rule: ContentChecksPolicyRuleModel;
}

export const CONTENT_CHECKS_RULE_TYPE_PICKER_MODAL = new UmbModalToken<
    ContentChecksRuleTypePickerModalData,
    ContentChecksRuleTypePickerModalValue
>("ContentChecks.Modal.RuleTypePicker", {
    modal: {
        type: "sidebar",
        size: "small",
    },
});
