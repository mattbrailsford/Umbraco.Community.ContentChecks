import type { ManifestMenu } from "@umbraco-cms/backoffice/menu";
import { CONTENT_CHECKS_GUARD_MENU_ALIAS, CONTENT_CHECKS_MENU_ALIAS } from "../constants.js";

export const sectionMenuManifests: ManifestMenu[] = [
    {
        type: "menu",
        alias: CONTENT_CHECKS_MENU_ALIAS,
        name: "Content Checks Menu",
    },
    {
        type: "menu",
        alias: CONTENT_CHECKS_GUARD_MENU_ALIAS,
        name: "Content Checks Guard Menu",
    },
];
