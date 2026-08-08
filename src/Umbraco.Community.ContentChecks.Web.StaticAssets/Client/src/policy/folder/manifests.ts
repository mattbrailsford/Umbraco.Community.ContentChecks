import { CONTENT_CHECKS_FOLDER_MOVE_REPOSITORY_ALIAS, CONTENT_CHECKS_FOLDER_REPOSITORY_ALIAS } from "./constants.js";
import { CONTENT_CHECKS_FOLDER_ENTITY_TYPE } from "../entity.js";
import { CONTENT_CHECKS_TREE_ALIAS, CONTENT_CHECKS_TREE_REPOSITORY_ALIAS } from "../tree/constants.js";

export const policyFolderManifests: Array<UmbExtensionManifest> = [
    {
        type: "repository",
        alias: CONTENT_CHECKS_FOLDER_REPOSITORY_ALIAS,
        name: "Content Checks Policy Folder Repository",
        api: () => import("./policy-folder.repository.js"),
    },
    {
        type: "store",
        alias: "ContentChecks.Store.PolicyFolder",
        name: "Content Checks Policy Folder Store",
        api: () => import("./policy-folder.store.js"),
    },
    {
        type: "repository",
        alias: CONTENT_CHECKS_FOLDER_MOVE_REPOSITORY_ALIAS,
        name: "Content Checks Policy Folder Move Repository",
        api: () => import("./move/policy-folder-move.repository.js"),
    },
    {
        type: "entityAction",
        kind: "folderUpdate",
        alias: "ContentChecks.EntityAction.PolicyFolder.Rename",
        name: "Rename Policy Folder",
        forEntityTypes: [CONTENT_CHECKS_FOLDER_ENTITY_TYPE],
        meta: { folderRepositoryAlias: CONTENT_CHECKS_FOLDER_REPOSITORY_ALIAS },
    },
    {
        type: "entityAction",
        kind: "moveTo",
        alias: "ContentChecks.EntityAction.PolicyFolder.MoveTo",
        name: "Move Policy Folder",
        forEntityTypes: [CONTENT_CHECKS_FOLDER_ENTITY_TYPE],
        meta: {
            treeRepositoryAlias: CONTENT_CHECKS_TREE_REPOSITORY_ALIAS,
            moveRepositoryAlias: CONTENT_CHECKS_FOLDER_MOVE_REPOSITORY_ALIAS,
            treeAlias: CONTENT_CHECKS_TREE_ALIAS,
            foldersOnly: true,
        },
    },
    {
        type: "entityAction",
        kind: "folderDelete",
        alias: "ContentChecks.EntityAction.PolicyFolder.Delete",
        name: "Delete Policy Folder",
        forEntityTypes: [CONTENT_CHECKS_FOLDER_ENTITY_TYPE],
        meta: { folderRepositoryAlias: CONTENT_CHECKS_FOLDER_REPOSITORY_ALIAS },
    },
];
