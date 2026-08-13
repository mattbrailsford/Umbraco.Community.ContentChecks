import type { ManifestModal } from "@umbraco-cms/backoffice/modal";

export const guardRuleModalManifests: Array<ManifestModal> = [
    {
        type: "modal",
        alias: "ContentChecks.Modal.RuleEditor",
        name: "Content Checks Guard Rule Editor Modal",
        element: () => import("./rule-editor/rule-editor-modal.element.js"),
    },
];
