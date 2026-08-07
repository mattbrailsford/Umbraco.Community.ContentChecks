using Microsoft.AspNetCore.Authorization;

namespace Umbraco.Community.ContentChecks.Web.Authorization;

/// <summary>
/// Satisfied when the current backoffice user has Content Checks' own section
/// (<see cref="Core.ContentChecksConstants.Sections.ContentChecks"/>) in their allowed sections.
/// Umbraco's own equivalent built-in policies (<c>SectionAccessContent</c>, etc.) build on an
/// internal requirement type this package can't reuse, so this is our own, same shape.
/// </summary>
public sealed class ContentChecksSectionAccessRequirement : IAuthorizationRequirement;
