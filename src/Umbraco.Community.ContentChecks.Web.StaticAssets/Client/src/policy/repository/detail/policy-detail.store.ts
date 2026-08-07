import { UmbDetailStoreBase } from "@umbraco-cms/backoffice/store";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import type { ContentChecksPolicyDetailModel } from "../../types.js";

export const CONTENT_CHECKS_POLICY_DETAIL_STORE_CONTEXT = new UmbContextToken<ContentChecksPolicyDetailStore>(
    "ContentChecksPolicyDetailStore",
);

export class ContentChecksPolicyDetailStore extends UmbDetailStoreBase<ContentChecksPolicyDetailModel> {
    constructor(host: UmbControllerHost) {
        super(host, CONTENT_CHECKS_POLICY_DETAIL_STORE_CONTEXT.toString());
    }
}

export { ContentChecksPolicyDetailStore as api };
