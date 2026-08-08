import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import type { UmbMoveDataSource, UmbMoveToRequestArgs } from "@umbraco-cms/backoffice/tree";
import { PoliciesService } from "../../../api/sdk.gen.js";
import { ContentChecksPolicyTypeMapper } from "../../type-mapper.js";

/**
 * Move a policy to a different folder (or root) -- there's no dedicated move endpoint, this just
 * re-submits the policy's own PUT with a new `parentFolderKey`, the same field the policy editor
 * already updates for the "which folder is this in" concern.
 */
export class ContentChecksPolicyMoveServerDataSource implements UmbMoveDataSource {
    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    async moveTo(args: UmbMoveToRequestArgs) {
        const { data: existing, error: readError } = await tryExecute(
            this.#host,
            PoliciesService.getPoliciesByKey({ path: { key: args.unique } }),
        );

        if (readError || !existing) {
            return { error: readError };
        }

        const model = { ...ContentChecksPolicyTypeMapper.toDetailModel(existing), parentFolderKey: args.destination.unique };

        return tryExecute(
            this.#host,
            PoliciesService.putPoliciesByKey({
                path: { key: args.unique },
                body: ContentChecksPolicyTypeMapper.toRequest(model),
            }),
        );
    }
}

export default ContentChecksPolicyMoveServerDataSource;
