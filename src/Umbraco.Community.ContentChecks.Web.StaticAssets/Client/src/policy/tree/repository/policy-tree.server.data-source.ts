import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type {
    UmbTreeAncestorsOfRequestArgs,
    UmbTreeChildrenOfRequestArgs,
    UmbTreeDataSource,
    UmbTreeRootItemsRequestArgs,
} from "@umbraco-cms/backoffice/tree";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { TreeService } from "../../../api/sdk.gen.js";
import type { TreeItemResponseModel } from "../../../api/types.gen.js";
import { CONTENT_CHECKS_FOLDER_ENTITY_TYPE, CONTENT_CHECKS_POLICY_ENTITY_TYPE, CONTENT_CHECKS_POLICY_ROOT_ENTITY_TYPE } from "../../entity.js";
import type { ContentChecksTreeItemModel } from "../types.js";

/**
 * Tree data source against this project's own combined policy/folder tree endpoints -- not any
 * native Umbraco tree management API. Folders and policies are both mapped to the same
 * `ContentChecksTreeItemModel`, differentiated by `entityType` and `isFolder`.
 */
export class ContentChecksPolicyTreeServerDataSource
    implements UmbTreeDataSource<ContentChecksTreeItemModel>
{
    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    async getRootItems(_args: UmbTreeRootItemsRequestArgs) {
        return this.#getChildren(null);
    }

    async getChildrenOf(args: UmbTreeChildrenOfRequestArgs) {
        return this.#getChildren(args.parent.unique);
    }

    async #getChildren(parentKey: string | null) {
        const { data, error } = await tryExecute(
            this.#host,
            TreeService.getTreeChildren({ query: { parentKey: parentKey ?? undefined } }),
        );

        if (error || !data) {
            return { error };
        }

        const items = data.map((item) => this.#toTreeItem(item, parentKey));
        return { data: { total: items.length, totalBefore: 0, totalAfter: 0, items } };
    }

    async getAncestorsOf(args: UmbTreeAncestorsOfRequestArgs) {
        const { data, error } = await tryExecute(
            this.#host,
            TreeService.getTreeByKeyAncestors({ path: { key: args.treeItem.unique } }),
        );

        if (error || !data) {
            return { data: [] };
        }

        const items: ContentChecksTreeItemModel[] = data.map((folder, index) => ({
            unique: folder.key,
            entityType: CONTENT_CHECKS_FOLDER_ENTITY_TYPE,
            parent: {
                unique: index === 0 ? null : data[index - 1].key,
                entityType: index === 0 ? CONTENT_CHECKS_POLICY_ROOT_ENTITY_TYPE : CONTENT_CHECKS_FOLDER_ENTITY_TYPE,
            },
            name: folder.name,
            hasChildren: true,
            isFolder: true,
        }));

        return { data: items };
    }

    #toTreeItem(item: TreeItemResponseModel, parentKey: string | null): ContentChecksTreeItemModel {
        return {
            unique: item.key,
            entityType: item.isFolder ? CONTENT_CHECKS_FOLDER_ENTITY_TYPE : CONTENT_CHECKS_POLICY_ENTITY_TYPE,
            parent: {
                unique: parentKey,
                entityType: parentKey ? CONTENT_CHECKS_FOLDER_ENTITY_TYPE : CONTENT_CHECKS_POLICY_ROOT_ENTITY_TYPE,
            },
            name: item.name,
            hasChildren: item.hasChildren,
            isFolder: item.isFolder,
            // Folders always render as icon-folder regardless of this (see default-tree-item-card),
            // so this only actually matters for the policy items themselves.
            icon: item.isFolder ? undefined : "icon-document-medal",
        };
    }
}

export default ContentChecksPolicyTreeServerDataSource;
