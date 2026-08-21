import { customElement, html, property } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import type { UmbPropertyEditorUiElement } from "@umbraco-cms/backoffice/property-editor";
import type { ContentChecksGuardRuleModel } from "../../types.js";
import type { CcGuardRulesEditorElement } from "../../components/rules-editor/rules-editor.element.js";
import "../../components/rules-editor/rules-editor.element.js";

const elementName = "content-checks-guard-property-editor-ui-rules";

/**
 * Property editor UI wrapping the rules editor, so a guard's rules are just another
 * `<umb-property>` in the same dataset as the start node -- getting property-layout spacing and
 * labeling for free instead of a bespoke box. Mirrors the policy rules property editor UI.
 */
@customElement(elementName)
export class ContentChecksGuardPropertyEditorUiRulesElement extends UmbLitElement implements UmbPropertyEditorUiElement {
    @property({ type: Array })
    value: ContentChecksGuardRuleModel[] = [];

    #onChange(e: Event) {
        const rulesEditor = e.target as CcGuardRulesEditorElement;
        this.value = rulesEditor.rules;
        this.dispatchEvent(new UmbChangeEvent());
    }

    override render() {
        return html`<cc-guard-rules-editor .rules=${this.value ?? []} @change=${this.#onChange}></cc-guard-rules-editor>`;
    }
}

export default ContentChecksGuardPropertyEditorUiRulesElement;

declare global {
    interface HTMLElementTagNameMap {
        [elementName]: ContentChecksGuardPropertyEditorUiRulesElement;
    }
}
