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
    /// Creates an operation status result for the given status using a problem details builder.
    /// </summary>
    /// <typeparam name="TEnum">The operation status enum type.</typeparam>
    /// <param name="status">The operation status.</param>
    /// <param name="builder">A function that creates the action result using the problem details builder.</param>
    /// <returns>An IActionResult based on the operation status.</returns>
    protected static IActionResult OperationStatusResult<TEnum>(
        TEnum status,
        Func<ProblemDetailsBuilder, IActionResult> builder)
        where TEnum : Enum
        => builder(new ProblemDetailsBuilder().WithOperationStatus(status));
}
