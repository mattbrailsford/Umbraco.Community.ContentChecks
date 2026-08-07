import type { ManifestModal } from "@umbraco-cms/backoffice/modal";

export const policyRuleModalManifests: Array<ManifestModal> = [
    {
        type: "modal",
        alias: "ContentChecks.Modal.RuleTypePicker",
        name: "Content Checks Rule Type Picker Modal",
        element: () => import("./rule-type-picker/rule-type-picker-modal.element.js"),
    },
    {
        type: "modal",
        alias: "ContentChecks.Modal.RuleConfigEditor",
        name: "Content Checks Rule Config Editor Modal",
        element: () => import("./rule-config-editor/rule-config-editor-modal.element.js"),
    },
];
