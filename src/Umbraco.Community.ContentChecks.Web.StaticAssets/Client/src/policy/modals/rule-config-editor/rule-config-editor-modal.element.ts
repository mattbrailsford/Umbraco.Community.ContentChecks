import { css, customElement, html, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbModalBaseElement } from "@umbraco-cms/backoffice/modal";
import type {
    ContentChecksRuleConfigEditorModalData,
    ContentChecksRuleConfigEditorModalValue,
} from "./rule-config-editor-modal.token.js";
import type { ContentChecksPolicyRuleModel } from "../../types.js";
import "../../../core/components/model-editor/model-editor.element.js";

const elementName = "content-checks-rule-config-editor-modal";

@customElement(elementName)
export class ContentChecksRuleConfigEditorModalElement extends UmbModalBaseElement<
    ContentChecksRuleConfigEditorModalData,
    ContentChecksRuleConfigEditorModalValue
> {
    @state()
    private _mode: ContentChecksPolicyRuleModel["mode"] = "Advisory";

    @state()
    private _notEvaluatedPolicy: ContentChecksPolicyRuleModel["notEvaluatedPolicy"] = "FailOpen";

    @state()
    private _configModel: Record<string, unknown> = {};

    override connectedCallback() {
        super.connectedCallback();

        if (this.data?.existingRule) {
            this._mode = this.data.existingRule.mode;
            this._notEvaluatedPolicy = this.data.existingRule.notEvaluatedPolicy;
            this._configModel = this.data.existingRule.config ? { ...this.data.existingRule.config } : {};
        }
    }

    #onModeChange(e: Event) {
        this._mode = (e.target as HTMLSelectElement).value as ContentChecksPolicyRuleModel["mode"];
    }

    #onNotEvaluatedPolicyChange(e: Event) {
        this._notEvaluatedPolicy = (e.target as HTMLSelectElement).value as ContentChecksPolicyRuleModel["notEvaluatedPolicy"];
    }

    #onConfigChange(e: CustomEvent<{ model: Record<string, unknown> }>) {
        this._configModel = e.detail.model;
    }

    #onSubmit() {
        const rule: ContentChecksPolicyRuleModel = {
            checkAlias: this.data!.checkType.alias,
            mode: this._mode,
            notEvaluatedPolicy: this._notEvaluatedPolicy,
            config: Object.keys(this._configModel).length > 0 ? this._configModel : null,
        };

        this.value = { rule };
        this.modalContext?.submit();
    }

    #onCancel() {
        this.modalContext?.reject();
    }

    override render() {
        const isEditing = !!this.data?.existingRule;
        const checkType = this.data?.checkType;
        const headline = isEditing ? `Edit ${checkType?.name ?? "Rule"}` : `Configure ${checkType?.name ?? "Rule"}`;

        return html`
            <umb-body-layout headline=${headline}>
                <div>
                    <uui-box headline="General">
                        <umb-property-layout label="Mode" description="Whether a failing check blocks saving or is only advisory.">
                            <div slot="editor">
                                <uui-select
                                    id="mode"
                                    .value=${this._mode}
                                    .options=${[
                                        { value: "Advisory", name: "Advisory", selected: this._mode === "Advisory" },
                                        { value: "Blocking", name: "Blocking", selected: this._mode === "Blocking" },
                                    ]}
                                    @change=${this.#onModeChange}
                                >
                                </uui-select>
                            </div>
                        </umb-property-layout>

                        <umb-property-layout
                            label="If not evaluated"
                            description="How to treat this rule if the check could not be evaluated."
                        >
                            <div slot="editor">
                                <uui-select
                                    id="notEvaluatedPolicy"
                                    .value=${this._notEvaluatedPolicy}
                                    .options=${[
                                        {
                                            value: "FailOpen",
                                            name: "Fail open (don't block)",
                                            selected: this._notEvaluatedPolicy === "FailOpen",
                                        },
                                        {
                                            value: "FailClosed",
                                            name: "Fail closed (treat as failed)",
                                            selected: this._notEvaluatedPolicy === "FailClosed",
                                        },
                                    ]}
                                    @change=${this.#onNotEvaluatedPolicyChange}
                                >
                                </uui-select>
                            </div>
                        </umb-property-layout>
                    </uui-box>

                    <cc-model-editor
                        .schema=${checkType?.configSchema}
                        .model=${this._configModel}
                        @change=${this.#onConfigChange}
                        empty-message="This check takes no configuration."
                        style="margin-top: var(--uui-size-layout-1);"
                    >
                    </cc-model-editor>
                </div>
                <div slot="actions">
                    <uui-button label="Cancel" @click=${this.#onCancel}>Cancel</uui-button>
                    <uui-button look="primary" color="positive" label=${isEditing ? "Save" : "Add"} @click=${this.#onSubmit}>
                        ${isEditing ? "Save" : "Add"}
                    </uui-button>
                </div>
            </umb-body-layout>
        `;
    }

    static override styles = [
        css`
            uui-box {
                --uui-box-default-padding: 0 var(--uui-size-space-5);
            }
            uui-box:not(:first-child) {
                margin-top: var(--uui-size-layout-1);
            }
            uui-select {
                width: 100%;
            }
        `,
    ];
}

export { ContentChecksRuleConfigEditorModalElement as element };

declare global {
    interface HTMLElementTagNameMap {
        [elementName]: ContentChecksRuleConfigEditorModalElement;
    }
}
