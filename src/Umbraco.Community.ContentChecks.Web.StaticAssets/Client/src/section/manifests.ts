import { sectionMenuManifests } from "./menu/manifests.js";
import { sectionSidebarManifests } from "./sidebar/manifests.js";
import { dashboardManifests } from "./dashboard/manifests.js";
import { CONTENT_CHECKS_SECTION_ALIAS } from "./constants.js";

const section: UmbExtensionManifest = {
    type: "section",
    alias: CONTENT_CHECKS_SECTION_ALIAS,
    name: "Content Checks Section",
    meta: { label: "Content Checks", pathname: "content-checks" },
    conditions: [{ alias: "Umb.Condition.SectionUserPermission", match: CONTENT_CHECKS_SECTION_ALIAS }],
};

export const sectionManifests: UmbExtensionManifest[] = [
    ...sectionMenuManifests,
    ...sectionSidebarManifests,
    ...dashboardManifests,
    section,
];
