import type { ManifestPropertyEditorUi } from "@umbraco-cms/backoffice/property-editor";

export const policyPropertyEditorManifests: Array<ManifestPropertyEditorUi> = [
    {
        type: "propertyEditorUi",
        alias: "ContentChecks.PropertyEditorUi.Rules",
        name: "Content Checks Rules Property Editor UI",
        element: () => import("./rules/property-editor-ui-rules.element.js"),
        meta: {
            label: "Rules",
            icon: "icon-circle-dotted-active",
            group: "Common",
        },
    },
];
