import { ContentChecksDeleteActionBase, type ContentChecksDeleteActionArgs } from "../../core/index.js";
import { ContentChecksPolicyDetailRepository } from "../repository/detail/policy-detail.repository.js";

export class ContentChecksDeletePolicyAction extends ContentChecksDeleteActionBase {
    protected getArgs(): ContentChecksDeleteActionArgs {
        return {
            headline: "#actions_delete",
            confirmMessage: "Are you sure you want to delete this policy?",
            getRepository: (host) => new ContentChecksPolicyDetailRepository(host),
        };
    }
}

export { ContentChecksDeletePolicyAction as api };
