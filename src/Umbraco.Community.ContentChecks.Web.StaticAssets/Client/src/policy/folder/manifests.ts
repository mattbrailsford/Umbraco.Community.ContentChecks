import { CONTENT_CHECKS_FOLDER_MOVE_REPOSITORY_ALIAS, CONTENT_CHECKS_FOLDER_REPOSITORY_ALIAS } from "./repository/constants.js";
import { policyFolderRepositoryManifests } from "./repository/manifests.js";
import { CONTENT_CHECKS_FOLDER_ENTITY_TYPE } from "../entity.js";
import { CONTENT_CHECKS_TREE_ALIAS } from "../tree/constants.js";
import { CONTENT_CHECKS_TREE_REPOSITORY_ALIAS } from "../tree/repository/constants.js";

export const policyFolderManifests: Array<UmbExtensionManifest> = [
    ...policyFolderRepositoryManifests,
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
