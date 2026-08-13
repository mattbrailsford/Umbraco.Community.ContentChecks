import { css, customElement, html, property, repeat, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import { UMB_MODAL_MANAGER_CONTEXT } from "@umbraco-cms/backoffice/modal";
import type { ContentChecksGuardRuleModel } from "../../types.js";
import { CONTENT_CHECKS_RULE_EDITOR_MODAL } from "../../modals/rule-editor/rule-editor-modal.token.js";

const elementName = "cc-guard-rules-editor";

/**
 * Editable list of a guard's rules -- a ref-node list where add/edit both open
 * the same rule-editor modal. Mirrors `CcPolicyRulesEditorElement`'s add/edit/remove shape, minus
 * the two-step type-picker-then-config flow (a rule has nothing to pick a "type" for).
 *
 * @fires change - Fired (as UmbChangeEvent) when the rule list changes; read `.rules` off the target.
 */
@customElement(elementName)
export class CcGuardRulesEditorElement extends UmbLitElement {
    @property({ type: Array })
    public get rules(): ContentChecksGuardRuleModel[] {
        return this._rules;
    }
    public set rules(value: ContentChecksGuardRuleModel[]) {
        this._rules = value ?? [];
    }

    @state()
    private _rules: ContentChecksGuardRuleModel[] = [];

    #emitChange() {
        this.dispatchEvent(new UmbChangeEvent());
    }

    async #onAdd() {
        const modalManager = await this.getContext(UMB_MODAL_MANAGER_CONTEXT);
        if (!modalManager) return;

        const modal = modalManager.open(this, CONTENT_CHECKS_RULE_EDITOR_MODAL, { data: {} });

        try {
            const result = await modal.onSubmit();
            if (!result?.rule) return;

            this._rules = [...this._rules, result.rule];
            this.#emitChange();
        } catch {
            // Modal was cancelled -- do nothing.
        }
    }

    async #onEdit(rule: ContentChecksGuardRuleModel, index: number) {
        const modalManager = await this.getContext(UMB_MODAL_MANAGER_CONTEXT);
        if (!modalManager) return;

        const modal = modalManager.open(this, CONTENT_CHECKS_RULE_EDITOR_MODAL, { data: { existingRule: rule } });

        try {
            const result = await modal.onSubmit();
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

    #nameFor(rule: ContentChecksGuardRuleModel): string {
        if (rule.userKey) return "Specific user";
        if (rule.roleKey) return "User group";
        return "Everyone else";
    }

    #summaryFor(rule: ContentChecksGuardRuleModel): string {
        return rule.blockAt ? `Blocks at: ${rule.blockAt}` : "Never blocks";
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

    #renderRule(rule: ContentChecksGuardRuleModel, index: number) {
        return html`
            <uui-ref-node name=${this.#nameFor(rule)} detail=${this.#summaryFor(rule)} @open=${() => this.#onEdit(rule, index)}>
                <umb-icon slot="icon" name="icon-user"></umb-icon>
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

export default CcGuardRulesEditorElement;

declare global {
    interface HTMLElementTagNameMap {
        [elementName]: CcGuardRulesEditorElement;
    }
}
