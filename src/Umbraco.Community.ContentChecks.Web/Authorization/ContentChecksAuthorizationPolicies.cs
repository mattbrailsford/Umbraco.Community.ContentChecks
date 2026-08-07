namespace Umbraco.Community.ContentChecks.Web.Authorization;

/// <summary>
/// Authorization policy names for the Content Checks Management API.
/// </summary>
public static class ContentChecksAuthorizationPolicies
{
    /// <summary>
    /// Requires the current backoffice user to have access to Content Checks' own top-level
    /// section (<see cref="Core.Constants.Sections.ContentChecks"/>) — a dedicated section, not
    /// Settings, so a content-editor user group can be granted exactly this and nothing else.
    /// </summary>
    public const string SectionAccessContentChecks = nameof(SectionAccessContentChecks);
}
