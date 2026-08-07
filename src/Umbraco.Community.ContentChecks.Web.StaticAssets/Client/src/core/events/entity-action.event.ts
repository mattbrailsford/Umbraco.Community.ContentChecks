import { UmbEntityActionEvent } from "@umbraco-cms/backoffice/entity-action";

/**
 * Custom event for Content Checks entity actions. Dispatched by repositories after successful
 * CRUD operations, so the tree and any open workspace can react.
 */
export class ContentChecksEntityActionEvent extends UmbEntityActionEvent {
    static readonly CREATED = "content-checks-entity-created";
    static readonly UPDATED = "content-checks-entity-updated";
    static readonly DELETED = "content-checks-entity-deleted";

    static created(unique: string, entityType: string) {
        return new ContentChecksEntityActionEvent(ContentChecksEntityActionEvent.CREATED, unique, entityType);
    }

    static updated(unique: string, entityType: string) {
        return new ContentChecksEntityActionEvent(ContentChecksEntityActionEvent.UPDATED, unique, entityType);
    }

    static deleted(unique: string, entityType: string) {
        return new ContentChecksEntityActionEvent(ContentChecksEntityActionEvent.DELETED, unique, entityType);
    }

    constructor(type: string, unique: string, entityType: string) {
        super(type, { unique, entityType });
    }
}
