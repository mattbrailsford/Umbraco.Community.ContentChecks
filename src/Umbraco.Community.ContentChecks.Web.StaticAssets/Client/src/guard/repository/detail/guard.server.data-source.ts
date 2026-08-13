import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { UmbDetailDataSource } from "@umbraco-cms/backoffice/repository";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { GuardsService } from "../../../api/sdk.gen.js";
import { ContentChecksGuardTypeMapper } from "../../type-mapper.js";
import type { ContentChecksGuardDetailModel } from "../../types.js";
import { CONTENT_CHECKS_GUARD_ENTITY_TYPE } from "../../entity.js";

/**
 * Server data source for guard CRUD.
 */
export class ContentChecksGuardDetailServerDataSource
    implements UmbDetailDataSource<ContentChecksGuardDetailModel>
{
    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    async createScaffold(preset?: Partial<ContentChecksGuardDetailModel>) {
        const scaffold: ContentChecksGuardDetailModel = {
            unique: crypto.randomUUID(),
            entityType: CONTENT_CHECKS_GUARD_ENTITY_TYPE,
            name: "",
            startNodeKey: null,
            rules: [],
            ...preset,
        };

        return { data: scaffold };
    }

    async read(unique: string) {
        const { data, error } = await tryExecute(
            this.#host,
            GuardsService.getGuardsByKey({ path: { key: unique } }),
        );
        if (error || !data) {
            return { error };
        }

        return { data: ContentChecksGuardTypeMapper.toDetailModel(data) };
    }

    async create(model: ContentChecksGuardDetailModel) {
        const requestBody = ContentChecksGuardTypeMapper.toRequest(model);

        const { data, error } = await tryExecute(
            this.#host,
            GuardsService.postGuards({ body: requestBody }),
        );

        if (error || !data) {
            return { error };
        }

        return { data: ContentChecksGuardTypeMapper.toDetailModel(data) };
    }

    async update(model: ContentChecksGuardDetailModel) {
        const requestBody = ContentChecksGuardTypeMapper.toRequest(model);

        const { data, error } = await tryExecute(
            this.#host,
            GuardsService.putGuardsByKey({ path: { key: model.unique }, body: requestBody }),
        );

        if (error || !data) {
            return { error };
        }

        return { data: ContentChecksGuardTypeMapper.toDetailModel(data) };
    }

    async delete(unique: string) {
        const { error } = await tryExecute(
            this.#host,
            GuardsService.deleteGuardsByKey({ path: { key: unique } }),
        );
        if (error) {
            return { error };
        }

        return {};
    }
}

export default ContentChecksGuardDetailServerDataSource;
