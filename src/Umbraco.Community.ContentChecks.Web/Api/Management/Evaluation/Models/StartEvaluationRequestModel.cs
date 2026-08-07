using System.ComponentModel.DataAnnotations;

namespace Umbraco.Community.ContentChecks.Web.Api.Management.Evaluation.Models;

/// <summary>
/// The content items to run checks against — a single key is just the N=1 case of the same
/// mechanism, not a separate endpoint or code path.
/// </summary>
public class StartEvaluationRequestModel
{
    /// <summary>
    /// The content items to evaluate. Capped at
    /// <see cref="Constants.ManagementApi.Feature.Evaluation.MaxBatchSize"/>.
    /// </summary>
    [Required]
    public IReadOnlyList<Guid> ContentKeys { get; set; } = [];
}
