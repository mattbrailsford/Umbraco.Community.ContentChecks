import {
    CONTENT_CHECKS_FOLDER_MOVE_REPOSITORY_ALIAS,
    CONTENT_CHECKS_FOLDER_REPOSITORY_ALIAS,
    CONTENT_CHECKS_FOLDER_STORE_ALIAS,
} from "./constants.js";

export const policyFolderRepositoryManifests: Array<UmbExtensionManifest> = [
    {
        type: "repository",
        alias: CONTENT_CHECKS_FOLDER_REPOSITORY_ALIAS,
        name: "Content Checks Policy Folder Repository",
        api: () => import("./detail/policy-folder.repository.js"),
    },
    {
        type: "store",
        alias: CONTENT_CHECKS_FOLDER_STORE_ALIAS,
        name: "Content Checks Policy Folder Store",
        api: () => import("./detail/policy-folder.store.js"),
    },
    {
        type: "repository",
        alias: CONTENT_CHECKS_FOLDER_MOVE_REPOSITORY_ALIAS,
        name: "Content Checks Policy Folder Move Repository",
        api: () => import("./move/policy-folder-move.repository.js"),
    },
];
