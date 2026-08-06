namespace Umbraco.Community.ContentChecks.Core.Checks;

/// <summary>
/// Where on the checked snapshot a result applies. Every field is optional — a target can be
/// "the whole item" (all null) up to "this exact property, this variant" (all populated).
/// Always relative to whichever snapshot the check was actually handed — a check never knows
/// whether that snapshot is the root document or one nested element, so it never needs to
/// express nesting here; the resolution layer that discovers nested elements owns that context.
/// </summary>
public sealed class ContentCheckTarget
{
    /// <summary>
    /// The property alias this result concerns, or <c>null</c> for the whole item.
    /// </summary>
    public string? PropertyAlias { get; init; }

    /// <summary>
    /// The culture this result concerns, or <c>null</c> for an invariant result.
    /// </summary>
    public string? Culture { get; init; }

    /// <summary>
    /// The segment this result concerns, or <c>null</c> for an unsegmented result.
    /// </summary>
    public string? Segment { get; init; }
}
