import { UmbSubmitWorkspaceAction, UMB_WORKSPACE_CONDITION_ALIAS } from "@umbraco-cms/backoffice/workspace";
import { CONTENT_CHECKS_GUARD_WORKSPACE_ALIAS } from "../constants.js";
import { CONTENT_CHECKS_GUARD_ENTITY_TYPE } from "../../entity.js";

export const guardWorkspaceManifests: Array<UmbExtensionManifest> = [
    {
        type: "workspace",
        kind: "routable",
        alias: CONTENT_CHECKS_GUARD_WORKSPACE_ALIAS,
        name: "Content Checks Guard Workspace",
        api: () => import("./guard-workspace.context.js"),
        meta: { entityType: CONTENT_CHECKS_GUARD_ENTITY_TYPE },
    },
    {
        type: "workspaceAction",
        kind: "default",
        alias: "ContentChecks.WorkspaceAction.Guard.Save",
        name: "Save Guard",
        api: UmbSubmitWorkspaceAction,
        meta: { label: "Save", look: "primary", color: "positive" },
        conditions: [{ alias: UMB_WORKSPACE_CONDITION_ALIAS, match: CONTENT_CHECKS_GUARD_WORKSPACE_ALIAS }],
    },
];
