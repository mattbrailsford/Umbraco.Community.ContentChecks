/**
 * Must match `Umbraco.Community.ContentChecks.Core.ContentChecksConstants.Sections.ContentChecks`
 * on the backend -- this is the same string a backoffice user's allowed-sections list must
 * contain for `ContentChecksSectionAccessRequirement` to grant Management API access.
 */
export const CONTENT_CHECKS_SECTION_ALIAS = "contentChecks";

export const CONTENT_CHECKS_SECTION_PATHNAME = "content-checks";

export const CONTENT_CHECKS_MENU_ALIAS = "ContentChecks.Menu.ContentChecks";

/**
 * A `menuWithEntityActions` sidebar app renders one header/create-button per menu, tied to a
 * single entity type -- it can't represent two different root entity types (Policy, Guard) under
 * one header. So Guards gets its own menu, sitting as a second,
 * separately-headed sidebar app alongside Policies', rather than sharing `CONTENT_CHECKS_MENU_ALIAS`.
 */
export const CONTENT_CHECKS_GUARD_MENU_ALIAS = "ContentChecks.Menu.Guard";
