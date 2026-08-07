import { UmbSubmitWorkspaceAction, UMB_WORKSPACE_CONDITION_ALIAS } from "@umbraco-cms/backoffice/workspace";
import { CONTENT_CHECKS_POLICY_WORKSPACE_ALIAS } from "../constants.js";
import { CONTENT_CHECKS_POLICY_ENTITY_TYPE } from "../../entity.js";

export const policyWorkspaceManifests: Array<UmbExtensionManifest> = [
    {
        type: "workspace",
        kind: "routable",
        alias: CONTENT_CHECKS_POLICY_WORKSPACE_ALIAS,
        name: "Content Checks Policy Workspace",
        api: () => import("./policy-workspace.context.js"),
        meta: { entityType: CONTENT_CHECKS_POLICY_ENTITY_TYPE },
    },
    {
        type: "workspaceAction",
        kind: "default",
        alias: "ContentChecks.WorkspaceAction.Policy.Save",
        name: "Save Policy",
        api: UmbSubmitWorkspaceAction,
        meta: { label: "Save", look: "primary", color: "positive" },
        conditions: [{ alias: UMB_WORKSPACE_CONDITION_ALIAS, match: CONTENT_CHECKS_POLICY_WORKSPACE_ALIAS }],
    },
];
