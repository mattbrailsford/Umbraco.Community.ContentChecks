namespace Umbraco.Community.ContentChecks.Persistence.Checks;

/// <summary>
/// EF Core-mapped counterpart of <see cref="Core.Checks.ContentCheckStoredResult"/>. One row per
/// rule, per resolved target, per content item — <see cref="PathJson"/> and <see cref="ResultJson"/>
/// are JSON columns rather than child tables since neither has identity or a lifecycle of its own
/// outside the row it belongs to.
/// </summary>
internal class ContentCheckResultEntity
{
    /// <summary>
    /// The row's own identity — generated fresh per result, not derived from anything the domain
    /// model carries, since a stored result has no natural key of its own until a future history
    /// feature adds a run id.
    /// </summary>
    public Guid Key { get; set; }

    /// <summary>
    /// The root content item this result belongs to.
    /// </summary>
    public Guid ContentKey { get; set; }

    /// <summary>
    /// The root content item's stamp as of the run that produced this result — lets a reader tell
    /// whether it still reflects the content as it currently stands. See
    /// <see cref="Core.Checks.ContentCheckStoredResult.ContentStamp"/>.
    /// </summary>
    public string? ContentStamp { get; set; }

    /// <summary>
    /// The culture the run was evaluated for, or <c>null</c> if the run wasn't culture-specific.
    /// </summary>
    public string? Culture { get; set; }

    /// <summary>
    /// The segment the run was evaluated for, or <c>null</c> if the run wasn't segment-specific.
    /// </summary>
    public string? Segment { get; set; }

    /// <summary>
    /// The result's location path, serialized as JSON.
    /// </summary>
    public string PathJson { get; set; } = "[]";

    /// <summary>
    /// The alias of the check that ran.
    /// </summary>
    public string CheckAlias { get; set; } = string.Empty;

    /// <summary>
    /// How important this rule's failure was, as of the run that produced this result.
    /// </summary>
    public Core.Checks.RequirementLevel RequirementLevel { get; set; }

    /// <summary>
    /// How a <c>NotEvaluated</c> result from this rule should be treated by a gate, as of the run
    /// that produced this result. See <see cref="Core.Policies.ContentCheckNotEvaluatedPolicy"/>.
    /// </summary>
    public Core.Policies.ContentCheckNotEvaluatedPolicy NotEvaluatedPolicy { get; set; }

    /// <summary>
    /// The check's result, serialized as JSON.
    /// </summary>
    public string ResultJson { get; set; } = "{}";

    /// <summary>
    /// When the run that produced this result completed, in UTC.
    /// </summary>
    public DateTime RanAtUtc { get; set; }
}
