import type { ManifestSectionSidebarApp } from "@umbraco-cms/backoffice/section";
import { CONTENT_CHECKS_MENU_ALIAS, CONTENT_CHECKS_SECTION_ALIAS } from "../constants.js";
import { CONTENT_CHECKS_POLICY_ROOT_ENTITY_TYPE } from "../../policy/entity.js";

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
];
