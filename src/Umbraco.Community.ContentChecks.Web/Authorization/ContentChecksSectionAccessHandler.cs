using Microsoft.AspNetCore.Authorization;
using Umbraco.Cms.Core.Security;
using Umbraco.Community.ContentChecks.Core;

namespace Umbraco.Community.ContentChecks.Web.Authorization;

/// <summary>
/// Default handler for <see cref="ContentChecksSectionAccessRequirement"/>.
/// </summary>
internal sealed class ContentChecksSectionAccessHandler : AuthorizationHandler<ContentChecksSectionAccessRequirement>
{
    private readonly IBackOfficeSecurityAccessor _backOfficeSecurityAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentChecksSectionAccessHandler"/> class.
    /// </summary>
    /// <param name="backOfficeSecurityAccessor">Resolves the current backoffice user.</param>
    public ContentChecksSectionAccessHandler(IBackOfficeSecurityAccessor backOfficeSecurityAccessor)
    {
        _backOfficeSecurityAccessor = backOfficeSecurityAccessor;
    }

    /// <inheritdoc />
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, ContentChecksSectionAccessRequirement requirement)
    {
        var currentUser = _backOfficeSecurityAccessor.BackOfficeSecurity?.CurrentUser;
        if (currentUser is not null && currentUser.AllowedSections.Contains(ContentChecksConstants.Sections.ContentChecks))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
