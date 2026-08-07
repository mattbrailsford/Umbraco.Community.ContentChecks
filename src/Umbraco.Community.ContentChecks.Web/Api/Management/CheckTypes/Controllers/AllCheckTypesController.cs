using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Community.ContentChecks.Core.Checks;
using Umbraco.Community.ContentChecks.Web.Api.Management.CheckTypes.Models;

namespace Umbraco.Community.ContentChecks.Web.Api.Management.CheckTypes.Controllers;

/// <summary>
/// Controller to get every registered check type, with its config schema — the UI renders every
/// check's config form from this, no per-type frontend code.
/// </summary>
[ApiVersion("1.0")]
public class AllCheckTypesController : CheckTypesControllerBase
{
    private readonly ContentCheckCollection _checks;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="AllCheckTypesController"/> class.
    /// </summary>
    /// <param name="checks">The registered check types.</param>
    /// <param name="umbracoMapper">Maps check types to API response models.</param>
    public AllCheckTypesController(ContentCheckCollection checks, IUmbracoMapper umbracoMapper)
    {
        _checks = checks;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Get every registered check type.
    /// </summary>
    /// <returns>Every registered check type.</returns>
    [HttpGet]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(IEnumerable<CheckTypeResponseModel>), StatusCodes.Status200OK)]
    public IActionResult GetAllCheckTypes()
        => Ok(_umbracoMapper.MapEnumerable<IContentCheck, CheckTypeResponseModel>(_checks));
}
