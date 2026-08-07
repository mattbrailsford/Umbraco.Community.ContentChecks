import {
    CONTENT_CHECKS_FOLDER_ENTITY_TYPE,
    CONTENT_CHECKS_POLICY_ENTITY_TYPE,
    CONTENT_CHECKS_POLICY_ROOT_ENTITY_TYPE,
} from "../entity.js";
import { CONTENT_CHECKS_FOLDER_REPOSITORY_ALIAS } from "../folder/constants.js";

const creatableEntityTypes = [CONTENT_CHECKS_POLICY_ROOT_ENTITY_TYPE, CONTENT_CHECKS_FOLDER_ENTITY_TYPE];

export const policyEntityActionManifests: Array<UmbExtensionManifest> = [
    {
        type: "entityAction",
        kind: "create",
        alias: "ContentChecks.EntityAction.Policy.Create",
        name: "Create Content Checks Item",
        weight: 1200,
        forEntityTypes: creatableEntityTypes,
    },
    {
        type: "entityCreateOptionAction",
        alias: "ContentChecks.EntityCreateOptionAction.Policy",
        name: "Create Policy Option",
        weight: 1000,
        api: () => import("./create-policy-option.action.js"),
        forEntityTypes: creatableEntityTypes,
        meta: { icon: "icon-checkbox-dotted-line", label: "Policy", description: "Attach a set of rules to a content type." },
    },
    {
        type: "entityCreateOptionAction",
        kind: "folder",
        alias: "ContentChecks.EntityCreateOptionAction.Folder",
        name: "Create Folder Option",
        weight: 900,
        forEntityTypes: creatableEntityTypes,
        meta: {
            icon: "icon-folder",
            label: "Folder",
            description: "Group related policies together.",
            folderRepositoryAlias: CONTENT_CHECKS_FOLDER_REPOSITORY_ALIAS,
        },
    },
    {
        type: "entityAction",
        kind: "default",
        alias: "ContentChecks.EntityAction.Policy.Delete",
        name: "Delete Policy",
        weight: 10,
        api: () => import("./delete-policy.action.js"),
        forEntityTypes: [CONTENT_CHECKS_POLICY_ENTITY_TYPE],
        meta: { icon: "icon-trash", label: "Delete" },
    },
];
