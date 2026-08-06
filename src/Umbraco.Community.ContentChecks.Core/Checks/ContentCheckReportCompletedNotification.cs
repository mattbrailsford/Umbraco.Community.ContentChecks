using Umbraco.Cms.Core.Notifications;

namespace Umbraco.Community.ContentChecks.Core.Checks;

/// <summary>
/// Published by <see cref="IContentCheckRunner.RunAsync"/> after every run — one snapshot, one
/// report. A general extensibility seam (logging, auditing, analytics, a fine-grained workflow
/// trigger) with no idea any particular subscriber exists. Not the notification a page-level
/// "did anything fail" consumer should use — see the aggregate notification Policy Resolution
/// fires once element-type cascading exists. Not cancelable: running checks and reporting the
/// result isn't a mutation anything downstream should be able to veto.
/// </summary>
public sealed class ContentCheckReportCompletedNotification : StatefulNotification
{
    /// <summary>
    /// The report the run produced.
    /// </summary>
    public required ContentCheckReport Report { get; init; }

    /// <summary>
    /// The key of the content that was checked.
    /// </summary>
    public required Guid ContentKey { get; init; }

    /// <summary>
    /// The content type alias of the content that was checked.
    /// </summary>
    public required string ContentTypeAlias { get; init; }

    /// <summary>
    /// The culture that was checked, or <c>null</c> for an invariant run.
    /// </summary>
    public string? Culture { get; init; }

    /// <summary>
    /// The segment that was checked, or <c>null</c> for an unsegmented run.
    /// </summary>
    public string? Segment { get; init; }
}
