import { UmbDetailStoreBase } from "@umbraco-cms/backoffice/store";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import type { ContentChecksGuardDetailModel } from "../../types.js";

export const CONTENT_CHECKS_GUARD_DETAIL_STORE_CONTEXT =
    new UmbContextToken<ContentChecksGuardDetailStore>("ContentChecksGuardDetailStore");

export class ContentChecksGuardDetailStore extends UmbDetailStoreBase<ContentChecksGuardDetailModel> {
    constructor(host: UmbControllerHost) {
        super(host, CONTENT_CHECKS_GUARD_DETAIL_STORE_CONTEXT.toString());
    }
}

export { ContentChecksGuardDetailStore as api };
