using Microsoft.AspNetCore.Authorization;

namespace Umbraco.Community.ContentChecks.Web.Authorization;

/// <summary>
/// Satisfied when the current backoffice user has Umbraco's own built-in Settings section
/// (<see cref="Umbraco.Cms.Core.Constants.Applications.Settings"/>) in their allowed sections.
/// Used to gate the Guards authoring surface, which per
/// docs/guard-spec.md should only be reachable by users who also have Settings
/// access — guard configuration is a site-configuration concern, not a content-editing one.
/// </summary>
public sealed class ContentChecksSettingsSectionAccessRequirement : IAuthorizationRequirement;
