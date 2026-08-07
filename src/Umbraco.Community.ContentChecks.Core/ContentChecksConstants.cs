namespace Umbraco.Community.ContentChecks.Core;

/// <summary>
/// Constants for Umbraco Content Checks. Named <c>ContentChecksConstants</c>, not the bare
/// <c>Constants</c> Umbraco.AI uses for its own equivalent, because this project's own files
/// routinely reference Umbraco CMS's own <see cref="Umbraco.Cms.Core.Constants"/> unqualified via
/// a <c>using Umbraco.Cms.Core;</c> — a same-named type in this namespace (or an ancestor of it)
/// would shadow that bare reference at every call site nested underneath.
/// </summary>
public static class ContentChecksConstants
{
    /// <summary>
    /// Section constants for Content Checks — usable for checking the current backoffice section,
    /// or for permission/authorization checks, without hardcoding the alias string everywhere.
    /// </summary>
    public static class Sections
    {
        /// <summary>
        /// The alias of Content Checks' own top-level backoffice section — decided in the Phase 3
        /// pre-design (see the Management API &amp; UI spec's "Where this lives in the backoffice"):
        /// a dedicated section, not nested in Settings, so a content-editor user group can be
        /// granted exactly this and nothing else.
        /// </summary>
        public const string ContentChecks = "contentChecks";
    }
}
