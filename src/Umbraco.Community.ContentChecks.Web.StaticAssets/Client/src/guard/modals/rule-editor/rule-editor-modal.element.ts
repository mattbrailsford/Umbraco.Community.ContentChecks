import { css, customElement, html, state, when } from "@umbraco-cms/backoffice/external/lit";
import { UmbModalBaseElement } from "@umbraco-cms/backoffice/modal";
import "@umbraco-cms/backoffice/user";
import "@umbraco-cms/backoffice/user-group";
import type { UmbUserInputElement } from "@umbraco-cms/backoffice/user";
import type { UmbUserGroupInputElement } from "@umbraco-cms/backoffice/user-group";
import type { RequirementLevel } from "../../../api/types.gen.js";
import type { ContentChecksGuardRuleModel } from "../../types.js";
import type { ContentChecksRuleEditorModalData, ContentChecksRuleEditorModalValue } from "./rule-editor-modal.token.js";

const elementName = "content-checks-rule-editor-modal";

type TargetKind = "everyoneElse" | "user" | "role";
type BlockAtOption = "none" | RequirementLevel;

/**
 * Add/edit modal for one guard rule -- a target (a specific user, a user group
 * ("role"), or neither for the catch-all rule) plus a single "block at" threshold: the lowest
 * requirement level that blocks. A failure at or above that level blocks; anything below it (and
 * everything, when set to "None") only warns.
 */
@customElement(elementName)
export class ContentChecksRuleEditorModalElement extends UmbModalBaseElement<
    ContentChecksRuleEditorModalData,
    ContentChecksRuleEditorModalValue
> {
    @state()
    private _targetKind: TargetKind = "everyoneElse";

    @state()
    private _userKey: string | null = null;

    @state()
    private _roleKey: string | null = null;

    @state()
    private _blockAt: BlockAtOption = "none";

    override connectedCallback() {
        super.connectedCallback();

        const existingRule = this.data?.existingRule;
        if (existingRule) {
            this._userKey = existingRule.userKey;
            this._roleKey = existingRule.roleKey;
            this._blockAt = existingRule.blockAt ?? "none";
            this._targetKind = existingRule.userKey ? "user" : existingRule.roleKey ? "role" : "everyoneElse";
        }
    }

    #onTargetKindChange(e: Event) {
        this._targetKind = (e.target as HTMLSelectElement).value as TargetKind;
        this._userKey = null;
        this._roleKey = null;
    }

    #onUserChange(e: Event) {
        const selection = (e.target as UmbUserInputElement).selection;
        this._userKey = selection[0] ?? null;
    }

    #onRoleChange(e: Event) {
        const selection = (e.target as UmbUserGroupInputElement).selection;
        this._roleKey = selection[0] ?? null;
    }

    #onBlockAtChange(e: Event) {
        this._blockAt = (e.target as HTMLSelectElement).value as BlockAtOption;
    }

    #onSubmit() {
        const rule: ContentChecksGuardRuleModel = {
            userKey: this._targetKind === "user" ? this._userKey : null,
            roleKey: this._targetKind === "role" ? this._roleKey : null,
            blockAt: this._blockAt === "none" ? null : this._blockAt,
        };

        this.value = { rule };
        this.modalContext?.submit();
    }

    #onCancel() {
        this.modalContext?.reject();
    }

    override render() {
        const isEditing = !!this.data?.existingRule;
        const headline = isEditing ? "Edit rule" : "Add rule";

        return html`
            <umb-body-layout headline=${headline}>
                <div>
                    <uui-box headline="Target">
                        <umb-property-layout
                            label="Applies to"
                            description="Who this rule applies to. A specific user beats a user group, which beats everyone else."
                        >
                            <div slot="editor">
                                <uui-select
                                    id="target-kind"
                                    .value=${this._targetKind}
                                    .options=${[
                                        { value: "everyoneElse", name: "Everyone else", selected: this._targetKind === "everyoneElse" },
                                        { value: "user", name: "A specific user", selected: this._targetKind === "user" },
                                        { value: "role", name: "A user group", selected: this._targetKind === "role" },
                                    ]}
                                    @change=${this.#onTargetKindChange}
                                >
                                </uui-select>
                            </div>
                        </umb-property-layout>

                        ${when(
                            this._targetKind === "user",
                            () => html`
                                <umb-property-layout label="User">
                                    <div slot="editor">
                                        <umb-user-input
                                            .selection=${this._userKey ? [this._userKey] : []}
                                            max="1"
                                            @change=${this.#onUserChange}
                                        ></umb-user-input>
                                    </div>
                                </umb-property-layout>
                            `,
                        )}

                        ${when(
                            this._targetKind === "role",
                            () => html`
                                <umb-property-layout label="User group">
                                    <div slot="editor">
                                        <umb-user-group-input
                                            .selection=${this._roleKey ? [this._roleKey] : []}
                                            max="1"
                                            @change=${this.#onRoleChange}
                                        ></umb-user-group-input>
                                    </div>
                                </umb-property-layout>
                            `,
                        )}
                    </uui-box>

                    <uui-box headline="Block at">
                        <umb-property-layout
                            label="Block at"
                            description="The lowest requirement level that blocks. A failing check at this level or higher blocks; anything below it only warns. Pick None to never block."
                        >
                            <div slot="editor">
                                <uui-select
                                    id="block-at"
                                    .value=${this._blockAt}
                                    .options=${[
                                        { value: "none", name: "None -- never block", selected: this._blockAt === "none" },
                                        { value: "Optional", name: "Optional", selected: this._blockAt === "Optional" },
                                        { value: "Recommended", name: "Recommended", selected: this._blockAt === "Recommended" },
                                        { value: "Mandatory", name: "Mandatory", selected: this._blockAt === "Mandatory" },
                                    ]}
                                    @change=${this.#onBlockAtChange}
                                >
                                </uui-select>
                            </div>
                        </umb-property-layout>
                    </uui-box>
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

export { ContentChecksRuleEditorModalElement as element };

declare global {
    interface HTMLElementTagNameMap {
        [elementName]: ContentChecksRuleEditorModalElement;
    }
}
