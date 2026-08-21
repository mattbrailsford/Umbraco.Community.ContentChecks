import type { GuardRequestModel, GuardResponseModel } from "../api/types.gen.js";
import { CONTENT_CHECKS_GUARD_ENTITY_TYPE } from "./entity.js";
import type { ContentChecksGuardDetailModel } from "./types.js";

export const ContentChecksGuardTypeMapper = {
    toDetailModel(response: GuardResponseModel): ContentChecksGuardDetailModel {
        return {
            unique: response.key,
            entityType: CONTENT_CHECKS_GUARD_ENTITY_TYPE,
            name: response.name,
            startNodeKey: response.startNodeKey ?? null,
            rules: (response.rules ?? []).map((rule) => ({
                key: rule.key,
                userKey: rule.userKey ?? null,
                roleKey: rule.roleKey ?? null,
                blockAt: rule.blockAt ?? null,
            })),
        };
    },

    toRequest(model: ContentChecksGuardDetailModel): GuardRequestModel {
        return {
            key: model.unique,
            name: model.name,
            startNodeKey: model.startNodeKey,
            rules: model.rules.map((rule) => ({
                key: rule.key,
                userKey: rule.userKey,
                roleKey: rule.roleKey,
                blockAt: rule.blockAt,
            })),
        };
    },
};
