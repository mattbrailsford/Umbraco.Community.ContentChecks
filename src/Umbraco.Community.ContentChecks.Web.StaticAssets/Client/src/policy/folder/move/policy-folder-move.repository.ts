import { UmbRepositoryBase } from "@umbraco-cms/backoffice/repository";
import { UMB_NOTIFICATION_CONTEXT } from "@umbraco-cms/backoffice/notification";
import type { UmbMoveRepository, UmbMoveToRequestArgs } from "@umbraco-cms/backoffice/tree";
import { ContentChecksPolicyFolderMoveServerDataSource } from "./policy-folder-move.server.data-source.js";

export class ContentChecksPolicyFolderMoveRepository extends UmbRepositoryBase implements UmbMoveRepository {
    #moveSource = new ContentChecksPolicyFolderMoveServerDataSource(this);

    async requestMoveTo(args: UmbMoveToRequestArgs) {
        const { error } = await this.#moveSource.moveTo(args);

        if (!error) {
            const notificationContext = await this.getContext(UMB_NOTIFICATION_CONTEXT);
            notificationContext?.peek("positive", { data: { message: "Moved" } });
        }

        return { error };
    }
}

export { ContentChecksPolicyFolderMoveRepository as api };
