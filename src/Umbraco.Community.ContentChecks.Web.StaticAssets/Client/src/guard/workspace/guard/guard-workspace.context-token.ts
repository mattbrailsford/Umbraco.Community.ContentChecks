import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import type { UmbSubmittableWorkspaceContext } from "@umbraco-cms/backoffice/workspace";
import { CONTENT_CHECKS_GUARD_ENTITY_TYPE } from "../../entity.js";
import type { ContentChecksGuardWorkspaceContext } from "./guard-workspace.context.js";

export const CONTENT_CHECKS_GUARD_WORKSPACE_CONTEXT = new UmbContextToken<
    UmbSubmittableWorkspaceContext,
    ContentChecksGuardWorkspaceContext
>(
    "UmbWorkspaceContext",
    undefined,
    (context): context is ContentChecksGuardWorkspaceContext =>
        context.getEntityType?.() === CONTENT_CHECKS_GUARD_ENTITY_TYPE,
);
