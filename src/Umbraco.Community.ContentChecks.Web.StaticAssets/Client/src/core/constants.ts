/**
 * Sentinel for "no folder" / "at the root" wherever a route needs a concrete string value rather
 * than an absent/optional segment (e.g. a create-workspace path's `:parentUnique`). Matches the
 * literal string Umbraco's own built-in `create` entity action kind falls back to
 * (`this.args.unique ?? "null"`) when there's no real parent unique -- using the same sentinel
 * means the built-in action's generated href and this project's own route parsing agree.
 */
export const CONTENT_CHECKS_ROOT_PARENT_UNIQUE = "null";
