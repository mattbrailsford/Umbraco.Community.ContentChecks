namespace Umbraco.Community.ContentChecks.Web.Authorization;

/// <summary>
/// Authorization policy names for the Content Checks Management API.
/// </summary>
public static class ContentChecksAuthorizationPolicies
{
    /// <summary>
    /// Requires the current backoffice user to have access to Content Checks' own top-level
    /// section (<see cref="Core.ContentChecksConstants.Sections.ContentChecks"/>) — a dedicated section, not
    /// Settings, so a content-editor user group can be granted exactly this and nothing else.
    /// </summary>
    public const string SectionAccessContentChecks = nameof(SectionAccessContentChecks);

    /// <summary>
    /// Requires the current backoffice user to also have access to Umbraco's own built-in
    /// Settings section — gates the Guards authoring surface, a
    /// site-configuration concern layered on top of section access, not a replacement for it.
    /// </summary>
    public const string SectionAccessSettings = nameof(SectionAccessSettings);
}
