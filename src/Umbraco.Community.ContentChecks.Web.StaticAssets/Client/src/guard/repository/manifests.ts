import {
    CONTENT_CHECKS_GUARD_DETAIL_REPOSITORY_ALIAS,
    CONTENT_CHECKS_GUARD_DETAIL_STORE_ALIAS,
} from "./constants.js";

export const guardRepositoryManifests: Array<UmbExtensionManifest> = [
    {
        type: "repository",
        alias: CONTENT_CHECKS_GUARD_DETAIL_REPOSITORY_ALIAS,
        name: "Content Checks Guard Detail Repository",
        api: () => import("./detail/guard.repository.js"),
    },
    {
        type: "store",
        alias: CONTENT_CHECKS_GUARD_DETAIL_STORE_ALIAS,
        name: "Content Checks Guard Detail Store",
        api: () => import("./detail/guard.store.js"),
    },
];
