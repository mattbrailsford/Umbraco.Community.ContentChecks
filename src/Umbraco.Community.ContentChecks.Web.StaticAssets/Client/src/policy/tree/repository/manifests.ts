import { CONTENT_CHECKS_TREE_REPOSITORY_ALIAS } from "./constants.js";

export const policyTreeRepositoryManifests: Array<UmbExtensionManifest> = [
    {
        type: "repository",
        alias: CONTENT_CHECKS_TREE_REPOSITORY_ALIAS,
        name: "Content Checks Policy Tree Repository",
        api: () => import("./policy-tree.repository.js"),
    },
];
