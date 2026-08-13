using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Core.Services;
using Umbraco.Community.ContentChecks.Core.Policies;
using Umbraco.Community.ContentChecks.Web.Api.Management.ApplicableChecks.Models;

namespace Umbraco.Community.ContentChecks.Web.Api.Management.ApplicableChecks.Controllers;

/// <summary>
/// Controller to get every check that applies to a real content item — resolution's own output
/// (Policy Resolution spec §7), not the schema-level Effective Checks service. This is what the
/// per-item panel renders as pending rows before anything has run.
/// </summary>
[ApiVersion("1.0")]
public class ByContentKeyApplicableChecksController : ApplicableChecksControllerBase
{
    private readonly IContentService _contentService;
    private readonly IContentCheckResolutionService _resolutionService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="ByContentKeyApplicableChecksController"/> class.
    /// </summary>
    /// <param name="contentService">Resolves a content item by key.</param>
    /// <param name="resolutionService">Resolves every applicable target for a content item.</param>
    /// <param name="umbracoMapper">Maps resolved targets to API response models.</param>
    public ByContentKeyApplicableChecksController(
        IContentService contentService,
        IContentCheckResolutionService resolutionService,
        IUmbracoMapper umbracoMapper)
    {
        _contentService = contentService;
        _resolutionService = resolutionService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Get every check that applies to a real content item.
    /// </summary>
    /// <param name="contentKey">The content item's key.</param>
    /// <param name="culture">The culture to read variant property values for.</param>
    /// <param name="segment">The segment to read segmented property values for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Every resolved target for this item, each with its matching policies.</returns>
    [HttpGet($"{{{nameof(contentKey)}}}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(IEnumerable<ApplicableCheckTargetResponseModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetApplicableChecks(
        Guid contentKey, string? culture = null, string? segment = null, CancellationToken cancellationToken = default)
    {
        var content = _contentService.GetById(contentKey);
        if (content is null)
        {
            return ContentNotFound();
        }

        var targets = await _resolutionService.ResolveAsync(content, culture, segment, cancellationToken);
        return Ok(_umbracoMapper.MapEnumerable<ContentCheckResolvedTarget, ApplicableCheckTargetResponseModel>(targets));
    }
}
