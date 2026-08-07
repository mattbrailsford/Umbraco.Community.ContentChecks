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
import { ContentChecksPolicyDetailRepository } from "../../repository/detail/policy-detail.repository.js";
import { CONTENT_CHECKS_POLICY_WORKSPACE_ALIAS } from "../constants.js";
import { CONTENT_CHECKS_POLICY_ENTITY_TYPE } from "../../entity.js";
import type { ContentChecksPolicyDetailModel } from "../../types.js";
import { ContentChecksPolicyWorkspaceEditorElement } from "./policy-workspace-editor.element.js";
import { CONTENT_CHECKS_ROOT_PARENT_UNIQUE } from "../../../core/constants.js";

/**
 * Workspace context for editing a check policy -- CRUD and state, no command-replay machinery
 * (unlike some sibling projects' equivalents): a policy's editable surface is small enough that
 * a plain "update this property" method is enough.
 */
export class ContentChecksPolicyWorkspaceContext
    extends UmbSubmittableWorkspaceContextBase<ContentChecksPolicyDetailModel>
    implements UmbRoutableWorkspaceContext
{
    readonly routes = new UmbWorkspaceRouteManager(this);

    #model = new UmbObjectState<ContentChecksPolicyDetailModel | undefined>(undefined);
    readonly model = this.#model.asObservable();
    readonly unique = this.model.pipe(map((model) => model?.unique));

    #repository: ContentChecksPolicyDetailRepository;
    #entityContext = new UmbEntityContext(this);

    constructor(host: UmbControllerHost) {
        super(host, CONTENT_CHECKS_POLICY_WORKSPACE_ALIAS);

        this.#repository = new ContentChecksPolicyDetailRepository(this);

        this.#entityContext.setEntityType(CONTENT_CHECKS_POLICY_ENTITY_TYPE);
        this.observe(this.model, (model) => this.#entityContext.setUnique(model?.unique ?? null));

        this.routes.setRoutes([
            {
                path: "create/parent/:parentUnique",
                component: ContentChecksPolicyWorkspaceEditorElement,
                setup: async (_component, info) => {
                    const parentUnique = info.match.params.parentUnique;
                    await this.scaffold(parentUnique === CONTENT_CHECKS_ROOT_PARENT_UNIQUE ? null : parentUnique);
                    new UmbWorkspaceIsNewRedirectController(
                        this,
                        this,
                        this.getHostElement().shadowRoot!.querySelector("umb-router-slot")!,
                    );
                },
            },
            {
                path: "edit/:unique",
                component: ContentChecksPolicyWorkspaceEditorElement,
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

    async scaffold(parentFolderKey: string | null) {
        this.resetState();
        const { data } = await this.#repository.createScaffold({ parentFolderKey });
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
    updateProperty<PropertyAlias extends keyof ContentChecksPolicyDetailModel>(
        propertyAlias: PropertyAlias,
        value: ContentChecksPolicyDetailModel[PropertyAlias],
    ) {
        const current = this.#model.getValue();
        if (!current) return;
        this.#model.setValue({ ...current, [propertyAlias]: value });
    }

    getData(): ContentChecksPolicyDetailModel | undefined {
        return this.#model.getValue();
    }

    getUnique(): string | undefined {
        return this.#model.getValue()?.unique;
    }

    getEntityType(): string {
        return CONTENT_CHECKS_POLICY_ENTITY_TYPE;
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

export { ContentChecksPolicyWorkspaceContext as api };
