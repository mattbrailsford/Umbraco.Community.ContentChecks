import { css, customElement, html, property, repeat, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import { UMB_MODAL_MANAGER_CONTEXT } from "@umbraco-cms/backoffice/modal";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { CheckTypesService } from "../../../api/sdk.gen.js";
import type { CheckTypeResponseModel } from "../../../api/types.gen.js";
import type { ContentChecksPolicyRuleModel } from "../../types.js";
import { CONTENT_CHECKS_RULE_TYPE_PICKER_MODAL } from "../../modals/rule-type-picker/rule-type-picker-modal.token.js";
import { CONTENT_CHECKS_RULE_CONFIG_EDITOR_MODAL } from "../../modals/rule-config-editor/rule-config-editor-modal.token.js";

const elementName = "cc-policy-rules-editor";

/**
 * Editable list of a policy's rules -- a ref-node list where adding opens a type-picker modal
 * (which check to run) followed by a config modal (mode/failure-policy plus the check's own
 * schema-driven fields); editing an existing rule reopens the config modal directly.
 *
 * @fires change - Fired (as UmbChangeEvent) when the rule list changes; read `.rules` off the target.
 */
@customElement(elementName)
export class CcPolicyRulesEditorElement extends UmbLitElement {
    @property({ type: Array })
    public get rules(): ContentChecksPolicyRuleModel[] {
        return this._rules;
    }
    public set rules(value: ContentChecksPolicyRuleModel[]) {
        this._rules = value ?? [];
    }

    @state()
    private _rules: ContentChecksPolicyRuleModel[] = [];

    @state()
    private _checkTypes: CheckTypeResponseModel[] = [];

    override connectedCallback() {
        super.connectedCallback();
        this.#loadCheckTypes();
    }

    async #loadCheckTypes() {
        const { data } = await tryExecute(this, CheckTypesService.getCheckTypes());
        this._checkTypes = data ?? [];
    }

    #checkTypeFor(checkAlias: string): CheckTypeResponseModel | undefined {
        return this._checkTypes.find((c) => c.alias === checkAlias);
    }

    #emitChange() {
        this.dispatchEvent(new UmbChangeEvent());
    }

    async #onAdd() {
        const modalManager = await this.getContext(UMB_MODAL_MANAGER_CONTEXT);
        if (!modalManager) return;

        const pickerModal = modalManager.open(this, CONTENT_CHECKS_RULE_TYPE_PICKER_MODAL, {
            data: { checkTypes: this._checkTypes },
        });

        const result = await pickerModal.onSubmit();
        if (!result?.rule) return;

        this._rules = [...this._rules, result.rule];
        this.#emitChange();
    }

    async #onEdit(rule: ContentChecksPolicyRuleModel, index: number) {
        const modalManager = await this.getContext(UMB_MODAL_MANAGER_CONTEXT);
        if (!modalManager) return;

        const checkType = this.#checkTypeFor(rule.checkAlias);
        if (!checkType) return;

        const configModal = modalManager.open(this, CONTENT_CHECKS_RULE_CONFIG_EDITOR_MODAL, {
            data: { checkType, existingRule: rule },
        });

        try {
            const result = await configModal.onSubmit();
            if (!result?.rule) return;

            this._rules = this._rules.map((r, i) => (i === index ? result.rule : r));
            this.#emitChange();
        } catch {
            // Modal was cancelled -- do nothing.
        }
    }

    #onRemove(index: number) {
        this._rules = this._rules.filter((_, i) => i !== index);
        this.#emitChange();
    }

    #summaryFor(rule: ContentChecksPolicyRuleModel): string {
        const notEvaluatedLabel = rule.notEvaluatedPolicy === "FailOpen" ? "fail open" : "fail closed";
        return `${rule.mode} · If not evaluated: ${notEvaluatedLabel}`;
    }

    override render() {
        return html`
            <uui-ref-list>
                ${repeat(
                    this._rules,
                    (_rule, index) => index,
                    (rule, index) => this.#renderRule(rule, index),
                )}
            </uui-ref-list>
            <uui-button id="btn-add" label="Add rule" look="placeholder" @click=${this.#onAdd}></uui-button>
        `;
    }

    #renderRule(rule: ContentChecksPolicyRuleModel, index: number) {
        const checkType = this.#checkTypeFor(rule.checkAlias);
        return html`
            <uui-ref-node
                name=${checkType?.name ?? rule.checkAlias}
                detail=${this.#summaryFor(rule)}
                @open=${() => this.#onEdit(rule, index)}
            >
                <umb-icon slot="icon" name="icon-checkbox"></umb-icon>
                <uui-action-bar slot="actions">
                    <uui-button label="Edit" @click=${() => this.#onEdit(rule, index)}>
                        <uui-icon name="icon-edit"></uui-icon>
                    </uui-button>
                    <uui-button
                        label="Remove"
                        @click=${(e: Event) => {
                            e.stopPropagation();
                            this.#onRemove(index);
                        }}
                    >
                        <uui-icon name="icon-trash"></uui-icon>
                    </uui-button>
                </uui-action-bar>
            </uui-ref-node>
        `;
    }

    static override styles = [
        css`
            uui-ref-node {
                padding-top: calc(var(--uui-size-2, 6px) + 5px);
                padding-bottom: calc(var(--uui-size-2, 6px) + 5px);
            }
            uui-ref-node::before {
                border-top: 1px solid var(--uui-color-divider-standalone);
            }
            #btn-add {
                width: 100%;
                margin-top: var(--uui-size-space-4);
            }
        `,
    ];
}

export default CcPolicyRulesEditorElement;

declare global {
    interface HTMLElementTagNameMap {
        [elementName]: CcPolicyRulesEditorElement;
    }
}
