using System.ComponentModel.DataAnnotations;

namespace Umbraco.Community.ContentChecks.Web.Api.Management.Evaluation.Models;

/// <summary>
/// The id of a newly-started run — returned immediately, before any content has been checked.
/// </summary>
public class StartEvaluationResponseModel
{
    /// <summary>
    /// The run's own identity. Poll <c>GET /evaluation/{runId}</c> or watch
    /// <c>GET /evaluation/{runId}/stream</c> for progress.
    /// </summary>
    [Required]
    public Guid RunId { get; set; }
}
