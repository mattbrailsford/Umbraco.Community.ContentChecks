using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Api.Common.Attributes;
using Umbraco.Cms.Api.Common.Builders;
using Umbraco.Cms.Api.Common.Filters;
using Umbraco.Cms.Web.Common.Authorization;
using Umbraco.Cms.Web.Common.Filters;
using Umbraco.Community.ContentChecks.Web.Authorization;

namespace Umbraco.Community.ContentChecks.Web.Api.Common.Controllers;

/// <summary>
/// Base controller for every Content Checks Management API controller.
/// </summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.BackOfficeAccess)]
[Authorize(Policy = ContentChecksAuthorizationPolicies.SectionAccessContentChecks)]
[DisableBrowserCache]
[Produces("application/json")]
[MapToApi(Constants.ManagementApi.ApiName)]
[JsonOptionsName(Constants.ManagementApi.ApiName)]
public abstract class ContentChecksManagementControllerBase : ControllerBase
{
    /// <summary>
    /// Creates a problem details object with the given title and detail.
    /// </summary>
    /// <param name="title">The title of the problem.</param>
    /// <param name="detail">The detail message of the problem.</param>
    /// <returns>A ProblemDetails object.</returns>
    protected static ProblemDetails CreateProblemDetails(string title, string detail)
        => new ProblemDetailsBuilder()
            .WithTitle(title)
            .WithDetail(detail)
            .Build();
}
