import type { ManifestPropertyEditorUi } from "@umbraco-cms/backoffice/property-editor";

export const guardPropertyEditorManifests: Array<ManifestPropertyEditorUi> = [
    {
        type: "propertyEditorUi",
        alias: "ContentChecks.PropertyEditorUi.GuardRules",
        name: "Content Checks Guard Rules Property Editor UI",
        element: () => import("./rules/property-editor-ui-rules.element.js"),
        meta: {
            label: "Rules",
            icon: "icon-shield",
            group: "Common",
        },
    },
];
