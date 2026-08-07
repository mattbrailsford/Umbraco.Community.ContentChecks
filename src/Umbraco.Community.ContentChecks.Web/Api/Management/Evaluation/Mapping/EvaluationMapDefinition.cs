using Umbraco.Cms.Core.Mapping;
using Umbraco.Community.ContentChecks.Web.Api.Management.Evaluation.Models;
using Umbraco.Community.ContentChecks.Web.Runs;

namespace Umbraco.Community.ContentChecks.Web.Api.Management.Evaluation.Mapping;

/// <summary>
/// Map definitions for evaluation run models. One-way only — <see cref="ContentCheckRunState"/>
/// and <see cref="ContentCheckItemRunState"/> are the live-run mechanism's own internal state;
/// these endpoints only ever read them.
/// </summary>
public class EvaluationMapDefinition : IMapDefinition
{
    /// <inheritdoc />
    public void DefineMaps(IUmbracoMapper mapper)
    {
        mapper.Define<ContentCheckRunState, EvaluationRunResponseModel>(
            (_, _) => new EvaluationRunResponseModel(), MapRunToResponse);
        mapper.Define<ContentCheckItemRunState, EvaluationItemResponseModel>(
            (_, _) => new EvaluationItemResponseModel(), MapItemToResponse);
    }

    // Umbraco.Code.MapAll
    private static void MapRunToResponse(ContentCheckRunState source, EvaluationRunResponseModel target, MapperContext context)
    {
        target.RunId = source.RunId;
        target.Status = source.Status;
        target.Items = context.MapEnumerable<ContentCheckItemRunState, EvaluationItemResponseModel>(source.Items.Values);
    }

    // Umbraco.Code.MapAll
    private static void MapItemToResponse(ContentCheckItemRunState source, EvaluationItemResponseModel target, MapperContext context)
    {
        target.ContentKey = source.ContentKey;
        target.Progress = source.Progress;
        target.Report = source.Report;
        target.Error = source.Error;
    }
}
