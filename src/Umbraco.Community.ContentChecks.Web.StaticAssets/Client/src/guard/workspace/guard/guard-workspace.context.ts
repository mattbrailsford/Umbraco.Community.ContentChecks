import type { UmbRoutableWorkspaceContext } from "@umbraco-cms/backoffice/workspace";
import {
    UmbWorkspaceRouteManager,
    UmbSubmittableWorkspaceContextBase,
    UmbWorkspaceIsNewRedirectController,
    UmbWorkspaceIsNewRedirectControllerAlias,
} from "@umbraco-cms/backoffice/workspace";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { map } from "@umbraco-cms/backoffice/external/rxjs";
import { UmbObjectState } from "@umbraco-cms/backoffice/observable-api";
import { UmbEntityContext } from "@umbraco-cms/backoffice/entity";
import { ContentChecksGuardDetailRepository } from "../../repository/detail/guard.repository.js";
import { CONTENT_CHECKS_GUARD_WORKSPACE_ALIAS } from "../constants.js";
import { CONTENT_CHECKS_GUARD_ENTITY_TYPE } from "../../entity.js";
import type { ContentChecksGuardDetailModel } from "../../types.js";
import { ContentChecksGuardWorkspaceEditorElement } from "./guard-workspace-editor.element.js";

/**
 * Workspace context for editing a guard -- CRUD and state. Flat list, so unlike
 * `ContentChecksPolicyWorkspaceContext`'s scaffold there's no parent folder to thread through.
 */
export class ContentChecksGuardWorkspaceContext
    extends UmbSubmittableWorkspaceContextBase<ContentChecksGuardDetailModel>
    implements UmbRoutableWorkspaceContext
{
    readonly routes = new UmbWorkspaceRouteManager(this);

    #model = new UmbObjectState<ContentChecksGuardDetailModel | undefined>(undefined);
    readonly model = this.#model.asObservable();
    readonly unique = this.model.pipe(map((model) => model?.unique));

    #repository: ContentChecksGuardDetailRepository;
    #entityContext = new UmbEntityContext(this);

    constructor(host: UmbControllerHost) {
        super(host, CONTENT_CHECKS_GUARD_WORKSPACE_ALIAS);

        this.#repository = new ContentChecksGuardDetailRepository(this);

        this.#entityContext.setEntityType(CONTENT_CHECKS_GUARD_ENTITY_TYPE);
        this.observe(this.model, (model) => this.#entityContext.setUnique(model?.unique ?? null));

        this.routes.setRoutes([
            {
                path: "create/parent/:parentUnique",
                component: ContentChecksGuardWorkspaceEditorElement,
                setup: async (_component) => {
                    await this.scaffold();
                    new UmbWorkspaceIsNewRedirectController(
                        this,
                        this,
                        this.getHostElement().shadowRoot!.querySelector("umb-router-slot")!,
                    );
                },
            },
            {
                path: "edit/:unique",
                component: ContentChecksGuardWorkspaceEditorElement,
                setup: (_component, info) => {
                    this.removeUmbControllerByAlias(UmbWorkspaceIsNewRedirectControllerAlias);
                    this.load(info.match.params.unique);
                },
            },
        ]);
    }

    protected resetState(): void {
        super.resetState();
        this.#model.setValue(undefined);
    }

    async scaffold() {
        this.resetState();
        const { data } = await this.#repository.createScaffold();
        if (data) {
            this.#model.setValue(data);
            this.setIsNew(true);
        }
    }

    async load(unique: string) {
        this.resetState();
        const { data, asObservable } = await this.#repository.requestByUnique(unique);

        if (asObservable) {
            this.observe(
                asObservable(),
                (model) => {
                    if (model) {
                        this.#model.setValue(structuredClone(model));
                        this.setIsNew(false);
                    }
                },
                "_observeModel",
            );
        }

        return data;
    }

    /**
     * Updates one property on the working model.
     */
    updateProperty<PropertyAlias extends keyof ContentChecksGuardDetailModel>(
        propertyAlias: PropertyAlias,
        value: ContentChecksGuardDetailModel[PropertyAlias],
    ) {
        const current = this.#model.getValue();
        if (!current) return;
        this.#model.setValue({ ...current, [propertyAlias]: value });
    }

    getData(): ContentChecksGuardDetailModel | undefined {
        return this.#model.getValue();
    }

    getUnique(): string | undefined {
        return this.#model.getValue()?.unique;
    }

    getEntityType(): string {
        return CONTENT_CHECKS_GUARD_ENTITY_TYPE;
    }

    async submit() {
        const model = this.#model.getValue();
        if (!model) return;

        if (this.getIsNew()) {
            const { data, error } = await this.#repository.create(model);
            if (error) throw error;
            if (data) this.#model.setValue(data);
        } else {
            const { data, error } = await this.#repository.save(model);
            if (error) throw error;
            if (data) this.#model.setValue(data);
        }

        this.setIsNew(false);
    }
}

export { ContentChecksGuardWorkspaceContext as api };
