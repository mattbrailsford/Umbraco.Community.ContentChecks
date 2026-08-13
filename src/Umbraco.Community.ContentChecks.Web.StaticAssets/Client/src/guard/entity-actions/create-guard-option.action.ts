import { UmbEntityCreateOptionActionBase } from "@umbraco-cms/backoffice/entity-create-option-action";
import { CONTENT_CHECKS_CREATE_GUARD_WORKSPACE_PATH_PATTERN } from "../workspace/guard/paths.js";
import { CONTENT_CHECKS_ROOT_PARENT_UNIQUE } from "../../core/constants.js";

export class ContentChecksCreateGuardOptionAction extends UmbEntityCreateOptionActionBase {
    async getHref() {
        return CONTENT_CHECKS_CREATE_GUARD_WORKSPACE_PATH_PATTERN.generateAbsolute({
            parentUnique: this.args.unique ?? CONTENT_CHECKS_ROOT_PARENT_UNIQUE,
        });
    }
}

export { ContentChecksCreateGuardOptionAction as api };
