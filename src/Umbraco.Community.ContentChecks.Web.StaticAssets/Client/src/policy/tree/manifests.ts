import { CONTENT_CHECKS_TREE_ALIAS, CONTENT_CHECKS_TREE_REPOSITORY_ALIAS } from "./constants.js";
import { CONTENT_CHECKS_FOLDER_ENTITY_TYPE, CONTENT_CHECKS_POLICY_ENTITY_TYPE } from "../entity.js";
import { CONTENT_CHECKS_MENU_ALIAS } from "../../section/constants.js";

export const policyTreeManifests: Array<UmbExtensionManifest> = [
    {
        type: "repository",
        alias: CONTENT_CHECKS_TREE_REPOSITORY_ALIAS,
        name: "Content Checks Policy Tree Repository",
        api: () => import("./policy-tree.repository.js"),
    },
    {
        type: "tree",
        kind: "default",
        alias: CONTENT_CHECKS_TREE_ALIAS,
        name: "Content Checks Policy Tree",
        api: () => import("./policy-tree.context.js"),
        element: () => import("./policy-tree.element.js"),
        meta: { repositoryAlias: CONTENT_CHECKS_TREE_REPOSITORY_ALIAS },
    },
    {
        type: "treeItem",
        kind: "default",
        alias: "ContentChecks.TreeItem.Policy",
        name: "Content Checks Policy Tree Item",
        forEntityTypes: [CONTENT_CHECKS_POLICY_ENTITY_TYPE],
    },
    {
        type: "treeItem",
        kind: "default",
        alias: "ContentChecks.TreeItem.PolicyFolder",
        name: "Content Checks Policy Folder Tree Item",
        forEntityTypes: [CONTENT_CHECKS_FOLDER_ENTITY_TYPE],
    },
    {
        type: "menuItem",
        kind: "tree",
        alias: "ContentChecks.MenuItem.Policies",
        name: "Content Checks Policies Menu Item",
        weight: 200,
        meta: {
            treeAlias: CONTENT_CHECKS_TREE_ALIAS,
            label: "Policies",
            menus: [CONTENT_CHECKS_MENU_ALIAS],
            hideTreeRoot: true,
        },
    },
];
