import { UMB_WORKSPACE_CONDITION_ALIAS } from "@umbraco-cms/backoffice/workspace";
import { UMB_DOCUMENT_WORKSPACE_ALIAS } from "@umbraco-cms/backoffice/document";
import { CONTENT_CHECKS_DOCUMENT_CHECKS_WORKSPACE_VIEW_ALIAS } from "./constants.js";

export const documentChecksManifests: Array<UmbExtensionManifest> = [
    {
        type: "workspaceView",
        alias: CONTENT_CHECKS_DOCUMENT_CHECKS_WORKSPACE_VIEW_ALIAS,
        name: "Content Checks Document Checks View",
        element: () => import("./document-checks-workspace-view.element.js"),
        weight: 90,
        meta: { label: "Checks", pathname: "content-checks", icon: "icon-circle-dotted-active" },
        conditions: [{ alias: UMB_WORKSPACE_CONDITION_ALIAS, match: UMB_DOCUMENT_WORKSPACE_ALIAS }],
    },
];
