import type { PolicyRequestModel, PolicyResponseModel } from "../api/types.gen.js";
import { CONTENT_CHECKS_POLICY_ENTITY_TYPE } from "./entity.js";
import type { ContentChecksPolicyDetailModel } from "./types.js";

export const ContentChecksPolicyTypeMapper = {
    toDetailModel(response: PolicyResponseModel): ContentChecksPolicyDetailModel {
        return {
            unique: response.key,
            entityType: CONTENT_CHECKS_POLICY_ENTITY_TYPE,
            alias: response.alias,
            name: response.name,
            contentTypeKey: response.contentTypeKey,
            startNodeKey: response.startNodeKey ?? null,
            parentFolderKey: response.parentFolderKey ?? null,
            rules: (response.rules ?? []).map((rule) => ({
                checkAlias: rule.checkAlias,
                config: (rule.config as Record<string, unknown> | null) ?? null,
                requirementLevel: rule.requirementLevel,
                notEvaluatedPolicy: rule.notEvaluatedPolicy,
            })),
        };
    },

    toRequest(model: ContentChecksPolicyDetailModel): PolicyRequestModel {
        return {
            key: model.unique,
            alias: model.alias,
            name: model.name,
            contentTypeKey: model.contentTypeKey ?? "",
            startNodeKey: model.startNodeKey,
            parentFolderKey: model.parentFolderKey,
            rules: model.rules.map((rule) => ({
                checkAlias: rule.checkAlias,
                config: rule.config,
                requirementLevel: rule.requirementLevel,
                notEvaluatedPolicy: rule.notEvaluatedPolicy,
            })),
        };
    },
};
