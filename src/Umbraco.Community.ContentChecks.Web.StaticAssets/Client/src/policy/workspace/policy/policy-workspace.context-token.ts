import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import type { UmbSubmittableWorkspaceContext } from "@umbraco-cms/backoffice/workspace";
import { CONTENT_CHECKS_POLICY_ENTITY_TYPE } from "../../entity.js";
import type { ContentChecksPolicyWorkspaceContext } from "./policy-workspace.context.js";

export const CONTENT_CHECKS_POLICY_WORKSPACE_CONTEXT = new UmbContextToken<
    UmbSubmittableWorkspaceContext,
    ContentChecksPolicyWorkspaceContext
>(
    "UmbWorkspaceContext",
    undefined,
    (context): context is ContentChecksPolicyWorkspaceContext =>
        context.getEntityType?.() === CONTENT_CHECKS_POLICY_ENTITY_TYPE,
);
