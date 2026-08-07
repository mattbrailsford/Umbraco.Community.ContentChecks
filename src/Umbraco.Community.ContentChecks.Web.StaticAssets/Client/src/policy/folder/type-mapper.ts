import type { FolderRequestModel, FolderResponseModel } from "../../api/types.gen.js";
import { CONTENT_CHECKS_FOLDER_ENTITY_TYPE } from "../entity.js";
import type { ContentChecksFolderDetailModel } from "../types.js";

export const ContentChecksFolderTypeMapper = {
    toDetailModel(response: FolderResponseModel): ContentChecksFolderDetailModel {
        return {
            unique: response.key,
            entityType: CONTENT_CHECKS_FOLDER_ENTITY_TYPE,
            name: response.name,
            parentFolderKey: response.parentFolderKey ?? null,
        };
    },

    toRequest(model: ContentChecksFolderDetailModel): FolderRequestModel {
        return {
            key: model.unique,
            name: model.name,
            parentFolderKey: model.parentFolderKey,
        };
    },
};
