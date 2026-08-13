namespace Umbraco.Community.ContentChecks.Core.Checks;

/// <summary>
/// One check's result from the most recently completed run, persisted so it can be shown without
/// a live run in progress. Stored per rule, per resolved target (root item or nested element) —
/// the same granularity a run itself produces — even though today only the latest run is kept, so
/// a later history feature doesn't need a shape change, only a new key (e.g. a run id) added
/// alongside <see cref="ContentKey"/>.
/// </summary>
public sealed class ContentCheckStoredResult
{
    /// <summary>
    /// The root content item this result belongs to. Always the root item's key, even for a
    /// result produced by a nested element — matches how a run is requested and cleared.
    /// </summary>
    public required Guid ContentKey { get; init; }

    /// <summary>
    /// The root content item's <see cref="IContentCheckSnapshot.ContentStamp"/> as of the run that
    /// produced this result, or <c>null</c> if the root snapshot couldn't provide one. Lets a
    /// reader tell whether this stored result still reflects the content as it currently stands —
    /// compare against the content's stamp right now; a mismatch means the content changed since
    /// this ran, and the result shouldn't be trusted (e.g. to decide whether to block a publish).
    /// </summary>
    public string? ContentStamp { get; init; }

    /// <summary>
    /// The culture the run was evaluated for, or <c>null</c> if the run wasn't culture-specific.
    /// </summary>
    public string? Culture { get; init; }

    /// <summary>
    /// The segment the run was evaluated for, or <c>null</c> if the run wasn't segment-specific.
    /// </summary>
    public string? Segment { get; init; }

    /// <summary>
    /// The root-to-here chain of nested elements the target that produced this result was
    /// discovered through. Empty means the root item itself.
    /// </summary>
    public required IReadOnlyList<Policies.ContentCheckLocationStep> Path { get; init; }

    /// <summary>
    /// The alias of the check that ran.
    /// </summary>
    public required string CheckAlias { get; init; }

    /// <summary>
    /// <see cref="Policies.ContentCheckPolicyRule.Key"/> of the rule that produced this result —
    /// distinguishes two rules that both ran <see cref="CheckAlias"/> at the same
    /// <see cref="Path"/> with different config (e.g. two "required property" rules checking
    /// different properties), which <see cref="CheckAlias"/> plus <see cref="Path"/> alone can't.
    /// </summary>
    public required Guid RuleKey { get; init; }

    /// <summary>
    /// The result the check produced.
    /// </summary>
    public required ContentCheckResult Result { get; init; }

    /// <summary>
    /// When the run that produced this result completed, in UTC.
    /// </summary>
    public required DateTime RanAtUtc { get; init; }
}
