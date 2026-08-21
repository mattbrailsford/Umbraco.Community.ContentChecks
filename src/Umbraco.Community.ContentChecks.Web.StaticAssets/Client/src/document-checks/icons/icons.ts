import type { UmbIconDictionary } from "@umbraco-cms/backoffice/icon";

export default [
    {
        name: "icon-content-checks-clipboard-check",
        path: () => import("./icon-clipboard-check.js"),
        keywords: ["clipboard", "check", "checklist", "checks", "content checks"],
    },
] satisfies UmbIconDictionary;
