import { UmbDefaultTreeContext } from "@umbraco-cms/backoffice/tree";
import type { UmbTreeItemModel, UmbTreeRootModel } from "@umbraco-cms/backoffice/tree";

export class ContentChecksGuardTreeContext extends UmbDefaultTreeContext<UmbTreeItemModel, UmbTreeRootModel> {}

export { ContentChecksGuardTreeContext as api };
