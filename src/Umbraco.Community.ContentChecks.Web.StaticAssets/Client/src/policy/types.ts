import type { UmbEntityModel } from "@umbraco-cms/backoffice/entity";
import type { ContentCheckNotEvaluatedPolicy, RequirementLevel } from "../api/types.gen.js";

/**
 * One rule within a policy.
 */
export interface ContentChecksPolicyRuleModel {
    checkAlias: string;
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
