using Microsoft.AspNetCore.Authorization;
using Umbraco.Cms.Core.Security;

namespace Umbraco.Community.ContentChecks.Web.Authorization;

/// <summary>
/// Default handler for <see cref="ContentChecksSettingsSectionAccessRequirement"/>.
/// </summary>
internal sealed class ContentChecksSettingsSectionAccessHandler : AuthorizationHandler<ContentChecksSettingsSectionAccessRequirement>
{
    private readonly IBackOfficeSecurityAccessor _backOfficeSecurityAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentChecksSettingsSectionAccessHandler"/> class.
    /// </summary>
    /// <param name="backOfficeSecurityAccessor">Resolves the current backoffice user.</param>
    public ContentChecksSettingsSectionAccessHandler(IBackOfficeSecurityAccessor backOfficeSecurityAccessor)
    {
        _backOfficeSecurityAccessor = backOfficeSecurityAccessor;
    }

    /// <inheritdoc />
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, ContentChecksSettingsSectionAccessRequirement requirement)
    {
        var currentUser = _backOfficeSecurityAccessor.BackOfficeSecurity?.CurrentUser;
        if (currentUser is not null && currentUser.AllowedSections.Contains(global::Umbraco.Cms.Core.Constants.Applications.Settings))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
