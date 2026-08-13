import type { ManifestSectionSidebarApp } from "@umbraco-cms/backoffice/section";
import { UMB_SETTINGS_SECTION_ALIAS } from "@umbraco-cms/backoffice/settings";
import {
    CONTENT_CHECKS_GUARD_MENU_ALIAS,
    CONTENT_CHECKS_MENU_ALIAS,
    CONTENT_CHECKS_SECTION_ALIAS,
} from "../constants.js";
import { CONTENT_CHECKS_POLICY_ROOT_ENTITY_TYPE } from "../../policy/entity.js";
import { CONTENT_CHECKS_GUARD_ROOT_ENTITY_TYPE } from "../../guard/entity.js";

/**
 * `menuWithEntityActions` renders one header + one create button per app instance, tied to a
 * single entity type -- so Policies and Guards each need their own sidebar app
 * (and their own menu, see `../menu/manifests.js`), not one shared header trying to represent
 * both root entity types at once.
 */
export const sectionSidebarManifests: ManifestSectionSidebarApp[] = [
    {
        type: "sectionSidebarApp",
        kind: "menuWithEntityActions",
        alias: "ContentChecks.SectionSidebarApp.Menu",
        name: "Content Checks Section Sidebar",
        weight: 1000,
        meta: { label: "Policies", menu: CONTENT_CHECKS_MENU_ALIAS, entityType: CONTENT_CHECKS_POLICY_ROOT_ENTITY_TYPE },
        conditions: [{ alias: "Umb.Condition.SectionAlias", match: CONTENT_CHECKS_SECTION_ALIAS }],
    },
    {
        type: "sectionSidebarApp",
        kind: "menuWithEntityActions",
        alias: "ContentChecks.SectionSidebarApp.GuardMenu",
        name: "Content Checks Guard Section Sidebar",
        weight: 900,
        meta: {
            label: "Guards",
            menu: CONTENT_CHECKS_GUARD_MENU_ALIAS,
            entityType: CONTENT_CHECKS_GUARD_ROOT_ENTITY_TYPE,
        },
        conditions: [
            { alias: "Umb.Condition.SectionAlias", match: CONTENT_CHECKS_SECTION_ALIAS },
            { alias: "Umb.Condition.SectionUserPermission", match: UMB_SETTINGS_SECTION_ALIAS },
        ],
    },
];
