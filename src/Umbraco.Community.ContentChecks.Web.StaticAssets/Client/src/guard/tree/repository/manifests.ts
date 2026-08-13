import { CONTENT_CHECKS_GUARD_TREE_REPOSITORY_ALIAS } from "./constants.js";

export const guardTreeRepositoryManifests: Array<UmbExtensionManifest> = [
    {
        type: "repository",
        alias: CONTENT_CHECKS_GUARD_TREE_REPOSITORY_ALIAS,
        name: "Content Checks Guard Tree Repository",
        api: () => import("./guard-tree.repository.js"),
    },
];
