import { CONTENT_CHECKS_POLICY_DETAIL_REPOSITORY_ALIAS, CONTENT_CHECKS_POLICY_DETAIL_STORE_ALIAS } from "./constants.js";

export const policyRepositoryManifests: Array<UmbExtensionManifest> = [
    {
        type: "repository",
        alias: CONTENT_CHECKS_POLICY_DETAIL_REPOSITORY_ALIAS,
        name: "Content Checks Policy Detail Repository",
        api: () => import("./detail/policy-detail.repository.js"),
    },
    {
        type: "store",
        alias: CONTENT_CHECKS_POLICY_DETAIL_STORE_ALIAS,
        name: "Content Checks Policy Detail Store",
        api: () => import("./detail/policy-detail.store.js"),
    },
];
