import { UMB_SETTINGS_SECTION_ALIAS } from "@umbraco-cms/backoffice/settings";
import { CONTENT_CHECKS_GUARD_ENTITY_TYPE, CONTENT_CHECKS_GUARD_ROOT_ENTITY_TYPE } from "../entity.js";

const creatableEntityTypes = [CONTENT_CHECKS_GUARD_ROOT_ENTITY_TYPE];

/**
 * Only the Settings-section check is needed here -- see `../tree/manifests.js` for why a second
 * `Umb.Condition.SectionUserPermission` matched against the Content Checks section would be both
 * redundant (the section itself already gates everything inside it) and broken (two instances of
 * the same condition type collide on their shared observer alias, killing the first one silently).
 */
const settingsGatedConditions = [{ alias: "Umb.Condition.SectionUserPermission", match: UMB_SETTINGS_SECTION_ALIAS }];

export const guardEntityActionManifests: Array<UmbExtensionManifest> = [
    {
        type: "entityAction",
        kind: "create",
        alias: "ContentChecks.EntityAction.Guard.Create",
        name: "Create Guard Item",
        weight: 1200,
        forEntityTypes: creatableEntityTypes,
        conditions: settingsGatedConditions,
    },
    {
        type: "entityCreateOptionAction",
        alias: "ContentChecks.EntityCreateOptionAction.Guard",
        name: "Create Guard Option",
        weight: 1000,
        api: () => import("./create-guard-option.action.js"),
        forEntityTypes: creatableEntityTypes,
        meta: {
            icon: "icon-shield",
            label: "Guard",
            description: "Decide whether a failing check warns or blocks, per user/role.",
        },
        conditions: settingsGatedConditions,
    },
    {
        type: "entityAction",
        kind: "default",
        alias: "ContentChecks.EntityAction.Guard.Delete",
        name: "Delete Guard",
        weight: 10,
        api: () => import("./delete-guard.action.js"),
        forEntityTypes: [CONTENT_CHECKS_GUARD_ENTITY_TYPE],
        meta: { icon: "icon-trash", label: "Delete" },
        conditions: settingsGatedConditions,
    },
];
