import type { UmbTreeItemModel, UmbTreeRootModel } from "@umbraco-cms/backoffice/tree";

export interface ContentChecksTreeItemModel extends UmbTreeItemModel {
    isFolder: boolean;
}

export type ContentChecksTreeRootModel = UmbTreeRootModel;
