import { UmbDetailRepositoryBase } from "@umbraco-cms/backoffice/repository";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbRequestReloadChildrenOfEntityEvent } from "@umbraco-cms/backoffice/entity-action";
import { ContentChecksPolicyDetailServerDataSource } from "./policy-detail.server.data-source.js";
import { CONTENT_CHECKS_POLICY_DETAIL_STORE_CONTEXT } from "./policy-detail.store.js";
import type { ContentChecksPolicyDetailModel } from "../../types.js";
import { CONTENT_CHECKS_FOLDER_ENTITY_TYPE, CONTENT_CHECKS_POLICY_ENTITY_TYPE, CONTENT_CHECKS_POLICY_ROOT_ENTITY_TYPE } from "../../entity.js";
import { ContentChecksEntityActionEvent, dispatchActionEvent } from "../../../core/index.js";

/**
 * Repository for check policy CRUD -- dispatches entity action events after successful
 * operations so the tree reloads the right node.
 */
export class ContentChecksPolicyDetailRepository extends UmbDetailRepositoryBase<ContentChecksPolicyDetailModel> {
    constructor(host: UmbControllerHost) {
        super(host, ContentChecksPolicyDetailServerDataSource, CONTENT_CHECKS_POLICY_DETAIL_STORE_CONTEXT);
    }

    override async create(model: ContentChecksPolicyDetailModel) {
        const result = await super.create(model, model.parentFolderKey ?? null);
        if (!result.error && result.data) {
            dispatchActionEvent(this, ContentChecksEntityActionEvent.created(result.data.unique, CONTENT_CHECKS_POLICY_ENTITY_TYPE));
            dispatchActionEvent(
                this,
                new UmbRequestReloadChildrenOfEntityEvent({
                    entityType: model.parentFolderKey ? CONTENT_CHECKS_FOLDER_ENTITY_TYPE : CONTENT_CHECKS_POLICY_ROOT_ENTITY_TYPE,
                    unique: model.parentFolderKey ?? null,
                }),
            );
        }
        return result;
    }

    override async save(model: ContentChecksPolicyDetailModel) {
        const result = await super.save(model);
        if (!result.error) {
            dispatchActionEvent(this, ContentChecksEntityActionEvent.updated(model.unique, CONTENT_CHECKS_POLICY_ENTITY_TYPE));
        }
        return result;
    }

    override async delete(unique: string) {
        const result = await super.delete(unique);
        if (!result.error) {
            dispatchActionEvent(this, ContentChecksEntityActionEvent.deleted(unique, CONTENT_CHECKS_POLICY_ENTITY_TYPE));
        }
        return result;
    }
}

export { ContentChecksPolicyDetailRepository as api };
