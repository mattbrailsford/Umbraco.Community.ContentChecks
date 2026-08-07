import { customElement, html, property } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import type { UmbPropertyEditorUiElement } from "@umbraco-cms/backoffice/property-editor";
import type { ContentChecksPolicyRuleModel } from "../../types.js";
import type { CcPolicyRulesEditorElement } from "../../components/policy-rules-editor/policy-rules-editor.element.js";
import "../../components/policy-rules-editor/policy-rules-editor.element.js";

const elementName = "content-checks-property-editor-ui-rules";

/**
 * Property editor UI wrapping the rules editor, so a policy's rules are just another
 * `<umb-property>` in the same dataset as content type/start node -- getting property-layout
 * spacing and labeling for free instead of a bespoke box.
 */
@customElement(elementName)
export class ContentChecksPropertyEditorUiRulesElement extends UmbLitElement implements UmbPropertyEditorUiElement {
    @property({ type: Array })
    value: ContentChecksPolicyRuleModel[] = [];

    #onChange(e: Event) {
        const rulesEditor = e.target as CcPolicyRulesEditorElement;
        this.value = rulesEditor.rules;
        this.dispatchEvent(new UmbChangeEvent());
    }

    override render() {
        return html`<cc-policy-rules-editor .rules=${this.value ?? []} @change=${this.#onChange}></cc-policy-rules-editor>`;
    }
}

export default ContentChecksPropertyEditorUiRulesElement;

declare global {
    interface HTMLElementTagNameMap {
        [elementName]: ContentChecksPropertyEditorUiRulesElement;
    }
}
