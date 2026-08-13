import { UMB_SETTINGS_SECTION_ALIAS } from "@umbraco-cms/backoffice/settings";
import { CONTENT_CHECKS_GUARD_TREE_ALIAS } from "./constants.js";
import { CONTENT_CHECKS_GUARD_TREE_REPOSITORY_ALIAS } from "./repository/constants.js";
import { guardTreeRepositoryManifests } from "./repository/manifests.js";
import { CONTENT_CHECKS_GUARD_ENTITY_TYPE } from "../entity.js";
import { CONTENT_CHECKS_GUARD_MENU_ALIAS } from "../../section/constants.js";

/**
 * Only the Settings-section check is needed here -- the Checks section's own manifest already
 * gates every extension inside it (including this one) on `Umb.Condition.SectionUserPermission`
 * matched against the Content Checks section, so repeating that check would be redundant. It would
 * also be broken: Umbraco's condition framework hosts every condition of one manifest on the same
 * controller host, and `UmbSectionUserPermissionCondition` registers its observer under a
 * module-level `Symbol()` alias -- a second instance of the *same* condition type on that host
 * collides with and silently kills the first one's observation. One condition per manifest, always.
 * Per docs/guard-spec.md: enforcement is a site-configuration concern, not a
 * content-editing one, so it's reachable only by a user who also has Umbraco's Settings section.
 */
const settingsGatedConditions = [{ alias: "Umb.Condition.SectionUserPermission", match: UMB_SETTINGS_SECTION_ALIAS }];

export const guardTreeManifests: Array<UmbExtensionManifest> = [
    ...guardTreeRepositoryManifests,
    {
        type: "tree",
        kind: "default",
        alias: CONTENT_CHECKS_GUARD_TREE_ALIAS,
        name: "Content Checks Guard Tree",
        api: () => import("./guard-tree.context.js"),
        element: () => import("./guard-tree.element.js"),
        meta: { repositoryAlias: CONTENT_CHECKS_GUARD_TREE_REPOSITORY_ALIAS },
        conditions: settingsGatedConditions,
    },
    {
        type: "treeItem",
        kind: "default",
        alias: "ContentChecks.TreeItem.Guard",
        name: "Content Checks Guard Tree Item",
        forEntityTypes: [CONTENT_CHECKS_GUARD_ENTITY_TYPE],
    },
    {
        type: "menuItem",
        kind: "tree",
        alias: "ContentChecks.MenuItem.Guards",
        name: "Content Checks Guards Menu Item",
        weight: 190,
        meta: {
            treeAlias: CONTENT_CHECKS_GUARD_TREE_ALIAS,
            label: "Guards",
            menus: [CONTENT_CHECKS_GUARD_MENU_ALIAS],
            hideTreeRoot: true,
        },
        conditions: settingsGatedConditions,
    },
];
