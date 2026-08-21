import type { UmbEntityModel } from "@umbraco-cms/backoffice/entity";
import type { RequirementLevel } from "../api/types.gen.js";

/**
 * One rule within a guard -- targets exactly one of a specific user, a specific
 * user group ("role"), or neither (the catch-all rule, "everyone else"). `blockAt` is the lowest
 * requirement level that blocks; `null` means never block.
 */
export interface ContentChecksGuardRuleModel {
    /** The rule's own durable identity. Empty for a rule not yet saved -- the server assigns one. */
    key: string;
    userKey: string | null;
    roleKey: string | null;
    blockAt: RequirementLevel | null;
}

/**
 * Detail model for workspace editing.
 */
export interface ContentChecksGuardDetailModel extends UmbEntityModel {
    unique: string;
    entityType: string;
    name: string;
    startNodeKey: string | null;
    rules: ContentChecksGuardRuleModel[];
}
