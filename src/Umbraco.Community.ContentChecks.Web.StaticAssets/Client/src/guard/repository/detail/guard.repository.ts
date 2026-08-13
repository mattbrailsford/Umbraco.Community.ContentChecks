import { UmbDetailRepositoryBase } from "@umbraco-cms/backoffice/repository";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { ContentChecksGuardDetailServerDataSource } from "./guard.server.data-source.js";
import { CONTENT_CHECKS_GUARD_DETAIL_STORE_CONTEXT } from "./guard.store.js";
import type { ContentChecksGuardDetailModel } from "../../types.js";
import { CONTENT_CHECKS_GUARD_ENTITY_TYPE, CONTENT_CHECKS_GUARD_ROOT_ENTITY_TYPE } from "../../entity.js";
import { ContentChecksEntityActionEvent, dispatchActionEvent } from "../../../core/index.js";
import { UmbRequestReloadChildrenOfEntityEvent } from "@umbraco-cms/backoffice/entity-action";

/**
 * Repository for guard CRUD -- flat list, no folders, so every create/delete
 * reloads the tree root rather than a parent folder. Dispatches entity action events after
 * successful operations, same as `ContentChecksPolicyDetailRepository`.
 */
export class ContentChecksGuardDetailRepository extends UmbDetailRepositoryBase<ContentChecksGuardDetailModel> {
    constructor(host: UmbControllerHost) {
        super(host, ContentChecksGuardDetailServerDataSource, CONTENT_CHECKS_GUARD_DETAIL_STORE_CONTEXT);
    }

    override async create(model: ContentChecksGuardDetailModel) {
        const result = await super.create(model, null);
        if (!result.error && result.data) {
            dispatchActionEvent(
                this,
                ContentChecksEntityActionEvent.created(result.data.unique, CONTENT_CHECKS_GUARD_ENTITY_TYPE),
            );
            dispatchActionEvent(
                this,
                new UmbRequestReloadChildrenOfEntityEvent({
                    entityType: CONTENT_CHECKS_GUARD_ROOT_ENTITY_TYPE,
                    unique: null,
                }),
            );
        }
        return result;
    }

    override async save(model: ContentChecksGuardDetailModel) {
        const result = await super.save(model);
        if (!result.error) {
            dispatchActionEvent(this, ContentChecksEntityActionEvent.updated(model.unique, CONTENT_CHECKS_GUARD_ENTITY_TYPE));
        }
        return result;
    }

    override async delete(unique: string) {
        const result = await super.delete(unique);
        if (!result.error) {
            dispatchActionEvent(this, ContentChecksEntityActionEvent.deleted(unique, CONTENT_CHECKS_GUARD_ENTITY_TYPE));
        }
        return result;
    }
}

export { ContentChecksGuardDetailRepository as api };
