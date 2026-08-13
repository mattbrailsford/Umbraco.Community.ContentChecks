import { ContentChecksDeleteActionBase, type ContentChecksDeleteActionArgs } from "../../core/index.js";
import { ContentChecksGuardDetailRepository } from "../repository/detail/guard.repository.js";

export class ContentChecksDeleteGuardAction extends ContentChecksDeleteActionBase {
    protected getArgs(): ContentChecksDeleteActionArgs {
        return {
            headline: "#actions_delete",
            confirmMessage: "Are you sure you want to delete this guard?",
            getRepository: (host) => new ContentChecksGuardDetailRepository(host),
        };
    }
}

export { ContentChecksDeleteGuardAction as api };
