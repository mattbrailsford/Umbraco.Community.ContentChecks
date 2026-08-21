import type { UmbEntityModel } from "@umbraco-cms/backoffice/entity";
import type { ContentCheckNotEvaluatedPolicy, RequirementLevel } from "../api/types.gen.js";

/**
 * One rule within a policy.
 */
export interface ContentChecksPolicyRuleModel {
    /** The rule's own durable identity. Empty for a rule not yet saved -- the server assigns one. */
    key: string;
    checkAlias: string;
    /** An editor-supplied display title, e.g. "Meta description required" -- falls back to the check type's own name when not set. */
    name: string | null;
    config: Record<string, unknown> | null;
    requirementLevel: RequirementLevel;
    notEvaluatedPolicy: ContentCheckNotEvaluatedPolicy;
}

/**
 * Detail model for workspace editing.
 */
export interface ContentChecksPolicyDetailModel extends UmbEntityModel {
    unique: string;
    entityType: string;
    alias: string;
    name: string;
    contentTypeKey: string | null;
    startNodeKey: string | null;
    parentFolderKey: string | null;
    rules: ContentChecksPolicyRuleModel[];
}

/**
 * Detail model for folder workspace editing -- matches `UmbFolderModel`'s own shape
 * (`unique`/`entityType`/`name`, plus the one field it doesn't have: `parentFolderKey`,
 * needed for move).
 */
export interface ContentChecksFolderDetailModel extends UmbEntityModel {
    unique: string;
    entityType: string;
    name: string;
    parentFolderKey: string | null;
}
