using System.ComponentModel.DataAnnotations;
using Umbraco.Community.ContentChecks.Core.Policies;

namespace Umbraco.Community.ContentChecks.Web.Api.Management.Evaluation.Models;

/// <summary>
/// One content item's own state within a run. Exactly one of <see cref="Report"/> or
/// <see cref="Error"/> is set once the item finishes; both are <c>null</c> while it's still running.
/// </summary>
public class EvaluationItemResponseModel
{
    /// <summary>
    /// The content item this state is for.
    /// </summary>
    [Required]
    public Guid ContentKey { get; set; }

    /// <summary>
    /// How many of this item's resolved targets have finished, as of the last update. <c>null</c>
    /// until the first one reports.
    /// </summary>
    public ContentCheckEvaluationProgress? Progress { get; set; }

    /// <summary>
    /// The item's finished report. <c>null</c> until evaluation completes successfully.
    /// </summary>
    public ContentCheckAggregateReport? Report { get; set; }

    /// <summary>
    /// Why this item failed — the content couldn't be found, or evaluation threw. <c>null</c>
    /// unless it did.
    /// </summary>
    public string? Error { get; set; }
}
