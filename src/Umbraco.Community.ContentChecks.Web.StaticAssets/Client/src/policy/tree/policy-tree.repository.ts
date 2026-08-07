import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbTreeRepositoryBase } from "@umbraco-cms/backoffice/tree";
import type { UmbApi } from "@umbraco-cms/backoffice/extension-api";
import { ContentChecksPolicyTreeServerDataSource } from "./policy-tree.server.data-source.js";
import { CONTENT_CHECKS_POLICY_ROOT_ENTITY_TYPE } from "../entity.js";
import type { ContentChecksTreeItemModel, ContentChecksTreeRootModel } from "./types.js";

export class ContentChecksPolicyTreeRepository
    extends UmbTreeRepositoryBase<ContentChecksTreeItemModel, ContentChecksTreeRootModel>
    implements UmbApi
{
    constructor(host: UmbControllerHost) {
        super(host, ContentChecksPolicyTreeServerDataSource);
    }

    async requestTreeRoot() {
        const { data: rootData } = await this._treeSource.getRootItems({ paging: { skip: 0, take: 0 } });
        const hasChildren = rootData ? rootData.total > 0 : false;
        return {
            data: {
                unique: null,
                entityType: CONTENT_CHECKS_POLICY_ROOT_ENTITY_TYPE,
                name: "Content Checks",
                hasChildren,
                isFolder: true,
            },
        };
    }
}

export default ContentChecksPolicyTreeRepository;
