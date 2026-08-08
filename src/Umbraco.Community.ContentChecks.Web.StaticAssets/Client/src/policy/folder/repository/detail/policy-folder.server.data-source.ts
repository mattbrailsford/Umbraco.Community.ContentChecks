import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { UmbDetailDataSource } from "@umbraco-cms/backoffice/repository";
import type { UmbFolderModel } from "@umbraco-cms/backoffice/tree";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { FoldersService } from "../../../../api/sdk.gen.js";
import { CONTENT_CHECKS_FOLDER_ENTITY_TYPE } from "../../../entity.js";

/**
 * Server data source for policy folder CRUD -- against this project's own custom folder
 * endpoints, not any native Umbraco tree/folder controller.
 */
export class ContentChecksPolicyFolderServerDataSource implements UmbDetailDataSource<UmbFolderModel> {
    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    async createScaffold(preset?: Partial<UmbFolderModel>) {
        return {
            data: {
                unique: crypto.randomUUID(),
                entityType: CONTENT_CHECKS_FOLDER_ENTITY_TYPE,
                name: "",
                ...preset,
            } as UmbFolderModel,
        };
    }

    async read(unique: string) {
        const { data, error } = await tryExecute(this.#host, FoldersService.getFoldersByKey({ path: { key: unique } }));
        if (error || !data) {
            return { error };
        }

        return { data: this.#toFolderModel(data.key, data.name) };
    }

    async create(model: UmbFolderModel, parentUnique: string | null) {
        const key = this.#requireUnique(model);
        const { data, error } = await tryExecute(
            this.#host,
            FoldersService.postFolders({ body: { key, name: model.name, parentFolderKey: parentUnique } }),
        );

        if (error || !data) {
            return { error };
        }

        return { data: this.#toFolderModel(data.key, data.name) };
    }

    async update(model: UmbFolderModel) {
        const key = this.#requireUnique(model);

        // UmbFolderModel (the built-in folderUpdate action's own payload shape) carries only
        // unique + name -- it has no room for parentFolderKey -- but this project's own PUT
        // endpoint takes the folder's full state. Read the existing parentFolderKey first so a
        // plain rename can't silently move the folder back to root.
        const existing = await tryExecute(this.#host, FoldersService.getFoldersByKey({ path: { key } }));
        if (existing.error) {
            return { error: existing.error };
        }

        const { data, error } = await tryExecute(
            this.#host,
            FoldersService.putFoldersByKey({
                path: { key },
                body: { key, name: model.name, parentFolderKey: existing.data?.parentFolderKey ?? null },
            }),
        );

        if (error || !data) {
            return { error };
        }

        return { data: this.#toFolderModel(data.key, data.name) };
    }

    async delete(unique: string) {
        const { error } = await tryExecute(this.#host, FoldersService.deleteFoldersByKey({ path: { key: unique } }));
        if (error) {
            return { error };
        }

        return {};
    }

    #toFolderModel(key: string, name: string): UmbFolderModel {
        return { unique: key, entityType: CONTENT_CHECKS_FOLDER_ENTITY_TYPE, name };
    }

    #requireUnique(model: UmbFolderModel): string {
        if (!model.unique) {
            throw new Error("A folder must have a unique identifier.");
        }
        return model.unique;
    }
}

export default ContentChecksPolicyFolderServerDataSource;
