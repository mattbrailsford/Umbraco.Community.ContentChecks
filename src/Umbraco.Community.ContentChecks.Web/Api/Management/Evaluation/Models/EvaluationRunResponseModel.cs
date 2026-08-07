using System.ComponentModel.DataAnnotations;
using Umbraco.Community.ContentChecks.Web.Runs;

namespace Umbraco.Community.ContentChecks.Web.Api.Management.Evaluation.Models;

/// <summary>
/// The full state of a run — one consistent shape regardless of how many items the run has. A
/// single-item run is just the N=1 case; its one item's completion is also the run's own
/// completion.
/// </summary>
public class EvaluationRunResponseModel
{
    /// <summary>
    /// The run's own identity.
    /// </summary>
    [Required]
    public Guid RunId { get; set; }

    /// <summary>
    /// Whether every item has finished yet.
    /// </summary>
    public ContentCheckRunStatus Status { get; set; }

    /// <summary>
    /// Every content item's own state.
    /// </summary>
    public IReadOnlyList<EvaluationItemResponseModel> Items { get; set; } = [];
}
