import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type {
    UmbTreeAncestorsOfRequestArgs,
    UmbTreeChildrenOfRequestArgs,
    UmbTreeDataSource,
    UmbTreeRootItemsRequestArgs,
} from "@umbraco-cms/backoffice/tree";
import type { UmbTreeItemModel } from "@umbraco-cms/backoffice/tree";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { GuardsService } from "../../../api/sdk.gen.js";
import { CONTENT_CHECKS_GUARD_ENTITY_TYPE, CONTENT_CHECKS_GUARD_ROOT_ENTITY_TYPE } from "../../entity.js";

/**
 * Tree data source for guards -- there's no dedicated tree endpoint on the
 * server (unlike policies, which have a real folder tree). Guards are a flat
 * list, so this data source just wraps the "get all" endpoint: root's children are every
 * guard, and no guard ever has children of its own.
 */
export class ContentChecksGuardTreeServerDataSource implements UmbTreeDataSource<UmbTreeItemModel> {
    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    async getRootItems(_args: UmbTreeRootItemsRequestArgs) {
        return this.#getChildren();
    }

    async getChildrenOf(args: UmbTreeChildrenOfRequestArgs) {
        // Flat list -- only the root ever has children.
        if (args.parent.unique !== null) {
            return { data: { total: 0, totalBefore: 0, totalAfter: 0, items: [] } };
        }

        return this.#getChildren();
    }

    async #getChildren() {
        const { data, error } = await tryExecute(this.#host, GuardsService.getGuards());

        if (error || !data) {
            return { error };
        }

        const items: UmbTreeItemModel[] = data.map((guard) => ({
            unique: guard.key,
            entityType: CONTENT_CHECKS_GUARD_ENTITY_TYPE,
            parent: { unique: null, entityType: CONTENT_CHECKS_GUARD_ROOT_ENTITY_TYPE },
            name: guard.name,
            hasChildren: false,
            isFolder: false,
        }));

        return { data: { total: items.length, totalBefore: 0, totalAfter: 0, items } };
    }

    async getAncestorsOf(_args: UmbTreeAncestorsOfRequestArgs) {
        // Flat list -- no ancestors above a guard other than the (unrepresented) root.
        return { data: [] };
    }
}

export default ContentChecksGuardTreeServerDataSource;
