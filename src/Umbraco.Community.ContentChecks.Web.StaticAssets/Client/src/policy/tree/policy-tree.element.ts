import { customElement } from "@umbraco-cms/backoffice/external/lit";
import { UmbDefaultTreeElement } from "@umbraco-cms/backoffice/tree";

@customElement("content-checks-policy-tree")
export class ContentChecksPolicyTreeElement extends UmbDefaultTreeElement {}

export default ContentChecksPolicyTreeElement;

declare global {
    interface HTMLElementTagNameMap {
        "content-checks-policy-tree": ContentChecksPolicyTreeElement;
    }
}
