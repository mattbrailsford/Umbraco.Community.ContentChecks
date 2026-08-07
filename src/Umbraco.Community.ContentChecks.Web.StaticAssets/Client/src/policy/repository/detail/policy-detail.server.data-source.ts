import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { UmbDetailDataSource } from "@umbraco-cms/backoffice/repository";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { PoliciesService } from "../../../api/sdk.gen.js";
import { ContentChecksPolicyTypeMapper } from "../../type-mapper.js";
import type { ContentChecksPolicyDetailModel } from "../../types.js";
import { CONTENT_CHECKS_POLICY_ENTITY_TYPE } from "../../entity.js";

/**
 * Server data source for check policy CRUD.
 */
export class ContentChecksPolicyDetailServerDataSource implements UmbDetailDataSource<ContentChecksPolicyDetailModel> {
    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    async createScaffold(preset?: Partial<ContentChecksPolicyDetailModel>) {
        const scaffold: ContentChecksPolicyDetailModel = {
            unique: crypto.randomUUID(),
            entityType: CONTENT_CHECKS_POLICY_ENTITY_TYPE,
            alias: "",
            name: "",
            contentTypeKey: null,
            startNodeKey: null,
            parentFolderKey: null,
            rules: [],
            ...preset,
        };

        return { data: scaffold };
    }

    async read(unique: string) {
        const { data, error } = await tryExecute(this.#host, PoliciesService.getPoliciesByKey({ path: { key: unique } }));
        if (error || !data) {
            return { error };
        }

        return { data: ContentChecksPolicyTypeMapper.toDetailModel(data) };
    }

    async create(model: ContentChecksPolicyDetailModel) {
        const requestBody = ContentChecksPolicyTypeMapper.toRequest(model);

        const { data, error } = await tryExecute(this.#host, PoliciesService.postPolicies({ body: requestBody }));

        if (error || !data) {
            return { error };
        }

        return { data: ContentChecksPolicyTypeMapper.toDetailModel(data) };
    }

    async update(model: ContentChecksPolicyDetailModel) {
        const requestBody = ContentChecksPolicyTypeMapper.toRequest(model);

        const { data, error } = await tryExecute(
            this.#host,
            PoliciesService.putPoliciesByKey({ path: { key: model.unique }, body: requestBody }),
        );

        if (error || !data) {
            return { error };
        }

        return { data: ContentChecksPolicyTypeMapper.toDetailModel(data) };
    }

    async delete(unique: string) {
        const { error } = await tryExecute(this.#host, PoliciesService.deletePoliciesByKey({ path: { key: unique } }));
        if (error) {
            return { error };
        }

        return {};
    }
}

export default ContentChecksPolicyDetailServerDataSource;
