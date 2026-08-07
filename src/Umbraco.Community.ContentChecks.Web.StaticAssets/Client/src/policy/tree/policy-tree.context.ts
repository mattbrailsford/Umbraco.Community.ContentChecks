import { UmbDefaultTreeContext } from "@umbraco-cms/backoffice/tree";
import type { ContentChecksTreeItemModel, ContentChecksTreeRootModel } from "./types.js";

export class ContentChecksPolicyTreeContext extends UmbDefaultTreeContext<
    ContentChecksTreeItemModel,
    ContentChecksTreeRootModel
> {}

export { ContentChecksPolicyTreeContext as api };
