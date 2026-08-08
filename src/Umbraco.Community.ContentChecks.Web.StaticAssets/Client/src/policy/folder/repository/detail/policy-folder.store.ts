import { UmbDetailStoreBase } from "@umbraco-cms/backoffice/store";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import type { UmbFolderModel } from "@umbraco-cms/backoffice/tree";

export const CONTENT_CHECKS_FOLDER_STORE_CONTEXT = new UmbContextToken<ContentChecksPolicyFolderStore>(
    "ContentChecksPolicyFolderStore",
);

export class ContentChecksPolicyFolderStore extends UmbDetailStoreBase<UmbFolderModel> {
    constructor(host: UmbControllerHost) {
        super(host, CONTENT_CHECKS_FOLDER_STORE_CONTEXT.toString());
    }
}

export { ContentChecksPolicyFolderStore as api };
