import { css, customElement, html, state, when } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import type { UmbPropertyDatasetElement, UmbPropertyValueData } from "@umbraco-cms/backoffice/property";
import { CONTENT_CHECKS_GUARD_WORKSPACE_ALIAS } from "../constants.js";
import { CONTENT_CHECKS_GUARD_WORKSPACE_CONTEXT } from "./guard-workspace.context-token.js";
import type { ContentChecksGuardWorkspaceContext } from "./guard-workspace.context.js";
import type { ContentChecksGuardDetailModel, ContentChecksGuardRuleModel } from "../../types.js";
import "../../components/rules-editor/rules-editor.element.js";

@customElement("content-checks-guard-workspace-editor")
export class ContentChecksGuardWorkspaceEditorElement extends UmbLitElement {
    #workspaceContext?: ContentChecksGuardWorkspaceContext;

    @state()
    private _model?: ContentChecksGuardDetailModel;

    constructor() {
        super();
        this.consumeContext(CONTENT_CHECKS_GUARD_WORKSPACE_CONTEXT, (context) => {
            this.#workspaceContext = context;
            this.observe(context?.model, (model) => (this._model = model), "_observeModel");
        });
    }

    #onNameChange(e: InputEvent) {
        this.#workspaceContext?.updateProperty("name", (e.target as HTMLInputElement).value);
    }

    #onStartNodeChange(e: CustomEvent) {
        e.stopPropagation();
        const dataset = e.target as UmbPropertyDatasetElement;
        const startNodeKey = dataset.value.find((v) => v.alias === "startNodeKey")?.value as string | undefined;
        this.#workspaceContext?.updateProperty("startNodeKey", startNodeKey || null);
    }

    #onRulesChange(e: CustomEvent) {
        e.stopPropagation();
        const rules = (e.target as HTMLElement & { rules: ContentChecksGuardRuleModel[] }).rules;
        this.#workspaceContext?.updateProperty("rules", rules ?? []);
    }

    #startNodeFieldValues(): UmbPropertyValueData[] {
        return [{ alias: "startNodeKey", value: this._model?.startNodeKey ?? undefined }];
    }

    override render() {
        if (!this._model) {
            return html`<uui-loader></uui-loader>`;
        }

        return html`
            <umb-workspace-editor alias=${CONTENT_CHECKS_GUARD_WORKSPACE_ALIAS}>
                <div id="header" slot="header">
                    <uui-input
                        id="name"
                        label="Name"
                        placeholder="Enter a name..."
                        .value=${this._model.name}
                        @input=${this.#onNameChange}
                        required
                    ></uui-input>
                </div>

                <div id="main">
                    <uui-box headline="Scope">
                        <umb-property-dataset .value=${this.#startNodeFieldValues()} @change=${this.#onStartNodeChange}>
                            <umb-property
                                label="Start node"
                                description="Restrict this guard to content underneath this node. Leave empty to apply to the whole site."
                                alias="startNodeKey"
                                property-editor-ui-alias="Umb.PropertyEditorUi.DocumentPicker"
                                .config=${[{ alias: "validationLimit", value: { min: 0, max: 1 } }]}
                            ></umb-property>
                        </umb-property-dataset>
                    </uui-box>

                    <uui-box headline="Rules">
                        <cc-guard-rules-editor
                            .rules=${this._model.rules}
                            @change=${this.#onRulesChange}
                        ></cc-guard-rules-editor>
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

export default ContentChecksGuardWorkspaceEditorElement;

declare global {
    interface HTMLElementTagNameMap {
        "content-checks-guard-workspace-editor": ContentChecksGuardWorkspaceEditorElement;
    }
}
