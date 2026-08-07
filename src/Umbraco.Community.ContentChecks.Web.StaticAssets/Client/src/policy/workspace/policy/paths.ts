import { UMB_WORKSPACE_PATH_PATTERN } from "@umbraco-cms/backoffice/workspace";
import { UmbPathPattern } from "@umbraco-cms/backoffice/router";
import { CONTENT_CHECKS_SECTION_PATHNAME } from "../../../section/constants.js";
import { CONTENT_CHECKS_POLICY_ENTITY_TYPE } from "../../entity.js";

export const CONTENT_CHECKS_POLICY_WORKSPACE_PATH = UMB_WORKSPACE_PATH_PATTERN.generateAbsolute({
    sectionName: CONTENT_CHECKS_SECTION_PATHNAME,
    entityType: CONTENT_CHECKS_POLICY_ENTITY_TYPE,
});

export const CONTENT_CHECKS_CREATE_POLICY_WORKSPACE_PATH_PATTERN = new UmbPathPattern<{ parentUnique: string }>(
    "create/parent/:parentUnique",
    CONTENT_CHECKS_POLICY_WORKSPACE_PATH,
);

export const CONTENT_CHECKS_EDIT_POLICY_WORKSPACE_PATH_PATTERN = new UmbPathPattern<{ unique: string }>(
    "edit/:unique",
    CONTENT_CHECKS_POLICY_WORKSPACE_PATH,
);
