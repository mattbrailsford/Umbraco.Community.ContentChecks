import { UMB_WORKSPACE_CONDITION_ALIAS } from "@umbraco-cms/backoffice/workspace";
import { UMB_DOCUMENT_WORKSPACE_ALIAS } from "@umbraco-cms/backoffice/document";
import {
    CONTENT_CHECKS_DOCUMENT_CHECKS_BADGE_WORKSPACE_CONTEXT_ALIAS,
    CONTENT_CHECKS_DOCUMENT_CHECKS_WORKSPACE_VIEW_ALIAS,
} from "./constants.js";

export const documentChecksManifests: Array<UmbExtensionManifest> = [
    {
        // Not one of Umbraco's own icons -- registered by the "icons" manifest below.
        type: "icons",
        alias: "ContentChecks.Icons.DocumentChecks",
        name: "Content Checks Icons",
        js: () => import("./icons/icons.js"),
    },
    {
        type: "workspaceView",
        alias: CONTENT_CHECKS_DOCUMENT_CHECKS_WORKSPACE_VIEW_ALIAS,
        name: "Content Checks Document Checks View",
        element: () => import("./document-checks-workspace-view.element.js"),
        weight: 90,
        meta: { label: "Checks", pathname: "content-checks", icon: "icon-content-checks-clipboard-check" },
        conditions: [{ alias: UMB_WORKSPACE_CONDITION_ALIAS, match: UMB_DOCUMENT_WORKSPACE_ALIAS }],
    },
    {
        // Alive for the whole document workspace, unlike the workspaceView above -- so the tab's
        // failure badge shows immediately on open, not only after a user has visited the tab once.
        type: "workspaceContext",
        alias: CONTENT_CHECKS_DOCUMENT_CHECKS_BADGE_WORKSPACE_CONTEXT_ALIAS,
        name: "Content Checks Document Checks Badge Workspace Context",
        api: () => import("./document-checks-badge.workspace-context.js"),
        conditions: [{ alias: UMB_WORKSPACE_CONDITION_ALIAS, match: UMB_DOCUMENT_WORKSPACE_ALIAS }],
    },
];
