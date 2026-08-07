import { css, customElement, html, state, when } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import type { UmbPropertyDatasetElement, UmbPropertyValueData } from "@umbraco-cms/backoffice/property";
import { CONTENT_CHECKS_POLICY_WORKSPACE_ALIAS } from "../constants.js";
import { CONTENT_CHECKS_POLICY_WORKSPACE_CONTEXT } from "./policy-workspace.context-token.js";
import type { ContentChecksPolicyWorkspaceContext } from "./policy-workspace.context.js";
import type { ContentChecksPolicyDetailModel, ContentChecksPolicyRuleModel } from "../../types.js";
import "../../property-editors/rules/property-editor-ui-rules.element.js";

@customElement("content-checks-policy-workspace-editor")
export class ContentChecksPolicyWorkspaceEditorElement extends UmbLitElement {
    #workspaceContext?: ContentChecksPolicyWorkspaceContext;

    @state()
    private _model?: ContentChecksPolicyDetailModel;

    constructor() {
        super();
        this.consumeContext(CONTENT_CHECKS_POLICY_WORKSPACE_CONTEXT, (context) => {
            this.#workspaceContext = context;
            this.observe(context?.model, (model) => (this._model = model), "_observeModel");
        });
    }

    #onNameChange(e: InputEvent) {
        const name = (e.target as HTMLInputElement).value;
        this.#workspaceContext?.updateProperty("name", name);
        if (!this._model?.alias) {
            this.#workspaceContext?.updateProperty("alias", this.#toAlias(name));
        }
    }

    #onAliasChange(e: InputEvent) {
        this.#workspaceContext?.updateProperty("alias", (e.target as HTMLInputElement).value);
    }

    #toAlias(name: string): string {
        return name
            .trim()
            .toLowerCase()
            .replace(/[^a-z0-9]+/g, "-")
            .replace(/(^-|-$)/g, "");
    }

    #onSchemaFieldsChange(e: CustomEvent) {
        e.stopPropagation();
        const dataset = e.target as UmbPropertyDatasetElement;
        const contentTypeKey = dataset.value.find((v) => v.alias === "contentTypeKey")?.value as string | undefined;
        const startNodeKey = dataset.value.find((v) => v.alias === "startNodeKey")?.value as string | undefined;
        this.#workspaceContext?.updateProperty("contentTypeKey", contentTypeKey || null);
        this.#workspaceContext?.updateProperty("startNodeKey", startNodeKey || null);
    }

    #onRulesChange(e: CustomEvent) {
        e.stopPropagation();
        const dataset = e.target as UmbPropertyDatasetElement;
        const rules = dataset.value.find((v) => v.alias === "rules")?.value as ContentChecksPolicyRuleModel[] | undefined;
        this.#workspaceContext?.updateProperty("rules", rules ?? []);
    }

    #schemaFieldValues(): UmbPropertyValueData[] {
        return [
            { alias: "contentTypeKey", value: this._model?.contentTypeKey ?? undefined },
            { alias: "startNodeKey", value: this._model?.startNodeKey ?? undefined },
        ];
    }

    #rulesFieldValues(): UmbPropertyValueData[] {
        return [{ alias: "rules", value: this._model?.rules ?? [] }];
    }

    override render() {
        if (!this._model) {
            return html`<uui-loader></uui-loader>`;
        }

        return html`
            <umb-workspace-editor alias=${CONTENT_CHECKS_POLICY_WORKSPACE_ALIAS}>
                <div id="header" slot="header">
                    <uui-input
                        id="name"
                        label="Name"
                        placeholder="Enter a name..."
                        .value=${this._model.name}
                        @input=${this.#onNameChange}
                        required
                    >
                        <uui-input-lock
                            slot="append"
                            id="alias"
                            label="Alias"
                            .value=${this._model.alias}
                            @input=${this.#onAliasChange}
                            required
                        ></uui-input-lock>
                    </uui-input>
                </div>

                <div id="main">
                    <uui-box headline="Content type">
                        <umb-property-dataset .value=${this.#schemaFieldValues()} @change=${this.#onSchemaFieldsChange}>
                            <umb-property
                                label="Content type"
                                description="Which document type or element type this policy attaches to, and optionally where."
                                alias="contentTypeKey"
                                property-editor-ui-alias="Umb.PropertyEditorUi.DocumentTypePicker"
                                .config=${[{ alias: "validationLimit", value: { min: 1, max: 1 } }]}
                                .validation=${{ mandatory: true }}
                            ></umb-property>
                            <umb-property
                                label="Start node"
                                description="Restrict this policy to content underneath this node. Leave empty to apply everywhere the content type reaches."
                                alias="startNodeKey"
                                property-editor-ui-alias="Umb.PropertyEditorUi.DocumentPicker"
                                .config=${[{ alias: "validationLimit", value: { min: 0, max: 1 } }]}
                            ></umb-property>
                        </umb-property-dataset>
                    </uui-box>

                    <uui-box headline="Rules">
                        <umb-property-dataset .value=${this.#rulesFieldValues()} @change=${this.#onRulesChange}>
                            <umb-property
                                label="Rules"
                                description="The checks this policy runs, and how strictly each is enforced."
                                alias="rules"
                                property-editor-ui-alias="ContentChecks.PropertyEditorUi.Rules"
                            ></umb-property>
                        </umb-property-dataset>
                    </uui-box>
                </div>

                ${when(
                    !this.#workspaceContext?.getIsNew(),
                    () => html`<umb-workspace-entity-action-menu slot="action-menu"></umb-workspace-entity-action-menu>`,
                )}
            </umb-workspace-editor>
        `;
    }

    static override styles = [
        UmbTextStyles,
        css`
            #header {
                display: flex;
                flex: 1;
                align-items: center;
                gap: var(--uui-size-space-4);
            }
            #name {
                width: 100%;
            }
            #main {
                padding: var(--uui-size-layout-1);
            }
            uui-box {
                margin-bottom: var(--uui-size-layout-1);
            }
        `,
    ];
}

export default ContentChecksPolicyWorkspaceEditorElement;

declare global {
    interface HTMLElementTagNameMap {
        "content-checks-policy-workspace-editor": ContentChecksPolicyWorkspaceEditorElement;
    }
}
