import { css, customElement, html, repeat, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbModalBaseElement, UMB_MODAL_MANAGER_CONTEXT } from "@umbraco-cms/backoffice/modal";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import type { CheckTypeResponseModel } from "../../../api/types.gen.js";
import type {
    ContentChecksRuleTypePickerModalData,
    ContentChecksRuleTypePickerModalValue,
} from "./rule-type-picker-modal.token.js";
import { CONTENT_CHECKS_RULE_CONFIG_EDITOR_MODAL } from "../rule-config-editor/rule-config-editor-modal.token.js";

const elementName = "content-checks-rule-type-picker-modal";

@customElement(elementName)
export class ContentChecksRuleTypePickerModalElement extends UmbModalBaseElement<
    ContentChecksRuleTypePickerModalData,
    ContentChecksRuleTypePickerModalValue
> {
    @state()
    private _checkTypes: CheckTypeResponseModel[] = [];

    override connectedCallback() {
        super.connectedCallback();
        this._checkTypes = this.data?.checkTypes ?? [];
    }

    async #handleSelect(checkType: CheckTypeResponseModel) {
        const modalManager = await this.getContext(UMB_MODAL_MANAGER_CONTEXT);
        if (!modalManager) return;

        const configModal = modalManager.open(this, CONTENT_CHECKS_RULE_CONFIG_EDITOR_MODAL, {
            data: { checkType, existingRule: undefined },
        });

        try {
            const configResult = await configModal.onSubmit();
            if (configResult?.rule) {
                this.modalContext?.setValue({ checkType, rule: configResult.rule });
                this.modalContext?.submit();
            }
        } catch {
            // Config modal was cancelled -- stay on the picker.
        }
    }

    override render() {
        return html`
            <umb-body-layout headline=${this.data?.headline ?? "Select Check"}>
                <div id="main">
                    <uui-box>
                        <uui-ref-list>
                            ${repeat(
                                this._checkTypes,
                                (type) => type.alias,
                                (type) => this.#renderCheckType(type),
                            )}
                        </uui-ref-list>
                    </uui-box>
                </div>
                <uui-button slot="actions" label=${this.localize.term("general_close")} @click=${this._rejectModal}>
                    ${this.localize.term("general_close")}
                </uui-button>
            </umb-body-layout>
        `;
    }

    #renderCheckType(type: CheckTypeResponseModel) {
        return html`
            <uui-ref-node name=${type.name} detail=${type.description ?? ""} @open=${() => this.#handleSelect(type)}>
                <umb-icon slot="icon" name="icon-checkbox-fill"></umb-icon>
            </uui-ref-node>
        `;
    }

    static override styles = [
        UmbTextStyles,
        css`
            :host {
                position: relative;
            }

            #main {
                display: flex;
                flex-direction: column;
                gap: var(--uui-size-space-5);
            }

            uui-ref-node {
                padding-top: calc(var(--uui-size-2, 6px) + 5px);
                padding-bottom: calc(var(--uui-size-2, 6px) + 5px);
            }

            uui-ref-node::before {
                border-top: 1px solid var(--uui-color-divider-standalone);
            }
        `,
    ];
}

export { ContentChecksRuleTypePickerModalElement as element };

declare global {
    interface HTMLElementTagNameMap {
        [elementName]: ContentChecksRuleTypePickerModalElement;
    }
}
