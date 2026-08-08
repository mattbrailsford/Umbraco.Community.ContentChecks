import { UmbDetailRepositoryBase } from "@umbraco-cms/backoffice/repository";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbRequestReloadChildrenOfEntityEvent } from "@umbraco-cms/backoffice/entity-action";
import type { UmbFolderModel } from "@umbraco-cms/backoffice/tree";
import { ContentChecksPolicyFolderServerDataSource } from "./policy-folder.server.data-source.js";
import { CONTENT_CHECKS_FOLDER_STORE_CONTEXT } from "./policy-folder.store.js";
import { CONTENT_CHECKS_FOLDER_ENTITY_TYPE, CONTENT_CHECKS_POLICY_ROOT_ENTITY_TYPE } from "../../../entity.js";
import { ContentChecksEntityActionEvent, dispatchActionEvent } from "../../../../core/index.js";

/**
 * Repository for policy folder CRUD -- dispatches entity action events after successful
 * operations so the tree reloads the right node.
 */
export class ContentChecksPolicyFolderRepository extends UmbDetailRepositoryBase<UmbFolderModel> {
    constructor(host: UmbControllerHost) {
        super(host, ContentChecksPolicyFolderServerDataSource, CONTENT_CHECKS_FOLDER_STORE_CONTEXT);
    }

    override async create(model: UmbFolderModel, parentUnique: string | null) {
        const result = await super.create(model, parentUnique);
        if (!result.error && result.data) {
            dispatchActionEvent(this, ContentChecksEntityActionEvent.created(result.data.unique!, CONTENT_CHECKS_FOLDER_ENTITY_TYPE));
            dispatchActionEvent(
                this,
                new UmbRequestReloadChildrenOfEntityEvent({
                    entityType: parentUnique ? CONTENT_CHECKS_FOLDER_ENTITY_TYPE : CONTENT_CHECKS_POLICY_ROOT_ENTITY_TYPE,
                    unique: parentUnique,
                }),
            );
        }
        return result;
    }

    override async save(model: UmbFolderModel) {
        const result = await super.save(model);
        if (!result.error) {
            dispatchActionEvent(this, ContentChecksEntityActionEvent.updated(model.unique!, CONTENT_CHECKS_FOLDER_ENTITY_TYPE));
        }
        return result;
    }

    override async delete(unique: string) {
        const result = await super.delete(unique);
        if (!result.error) {
            dispatchActionEvent(this, ContentChecksEntityActionEvent.deleted(unique, CONTENT_CHECKS_FOLDER_ENTITY_TYPE));
        }
        return result;
    }
}

export { ContentChecksPolicyFolderRepository as api };
