import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import type { UmbMoveDataSource, UmbMoveToRequestArgs } from "@umbraco-cms/backoffice/tree";
import { FoldersService } from "../../../api/sdk.gen.js";

/**
 * Move a folder to a different parent folder (or root) -- re-submits the folder's own PUT with a
 * new `parentFolderKey`, same as the rename data source but actually changing the parent instead
 * of preserving it.
 */
export class ContentChecksPolicyFolderMoveServerDataSource implements UmbMoveDataSource {
    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    async moveTo(args: UmbMoveToRequestArgs) {
        const { data: existing, error: readError } = await tryExecute(
            this.#host,
            FoldersService.getFoldersByKey({ path: { key: args.unique } }),
        );

        if (readError || !existing) {
            return { error: readError };
        }

        return tryExecute(
            this.#host,
            FoldersService.putFoldersByKey({
                path: { key: args.unique },
                body: { key: args.unique, name: existing.name, parentFolderKey: args.destination.unique },
            }),
        );
    }
}

export default ContentChecksPolicyFolderMoveServerDataSource;
