import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbTreeRepositoryBase } from "@umbraco-cms/backoffice/tree";
import type { UmbTreeItemModel, UmbTreeRootModel } from "@umbraco-cms/backoffice/tree";
import type { UmbApi } from "@umbraco-cms/backoffice/extension-api";
import { ContentChecksGuardTreeServerDataSource } from "./guard-tree.server.data-source.js";
import { CONTENT_CHECKS_GUARD_ROOT_ENTITY_TYPE } from "../../entity.js";

export class ContentChecksGuardTreeRepository
    extends UmbTreeRepositoryBase<UmbTreeItemModel, UmbTreeRootModel>
    implements UmbApi
{
    constructor(host: UmbControllerHost) {
        super(host, ContentChecksGuardTreeServerDataSource);
    }

    async requestTreeRoot() {
        const { data: rootData } = await this._treeSource.getRootItems({ paging: { skip: 0, take: 0 } });
        const hasChildren = rootData ? rootData.total > 0 : false;
        return {
            data: {
                unique: null,
                entityType: CONTENT_CHECKS_GUARD_ROOT_ENTITY_TYPE,
                name: "Guards",
                hasChildren,
                isFolder: true,
            },
        };
    }
}

export default ContentChecksGuardTreeRepository;
