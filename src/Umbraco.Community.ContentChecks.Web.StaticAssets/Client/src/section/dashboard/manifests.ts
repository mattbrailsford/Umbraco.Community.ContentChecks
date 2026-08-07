import { CONTENT_CHECKS_SECTION_ALIAS } from "../constants.js";

const dashboard: UmbExtensionManifest = {
    type: "dashboard",
    alias: "ContentChecks.Dashboard.Welcome",
    name: "Content Checks Welcome Dashboard",
    element: () => import("./welcome-dashboard.element.js"),
    weight: 10,
    meta: { label: "Welcome", pathname: "welcome" },
    conditions: [{ alias: "Umb.Condition.SectionAlias", match: CONTENT_CHECKS_SECTION_ALIAS }],
};

export const dashboardManifests: UmbExtensionManifest[] = [dashboard];
