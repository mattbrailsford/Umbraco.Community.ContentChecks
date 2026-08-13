import { customElement } from "@umbraco-cms/backoffice/external/lit";
import { UmbDefaultTreeElement } from "@umbraco-cms/backoffice/tree";

@customElement("content-checks-guard-tree")
export class ContentChecksGuardTreeElement extends UmbDefaultTreeElement {}

export default ContentChecksGuardTreeElement;

declare global {
    interface HTMLElementTagNameMap {
        "content-checks-guard-tree": ContentChecksGuardTreeElement;
    }
}
