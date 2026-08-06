using Umbraco.Cms.Core.Events;
using Umbraco.Community.ContentChecks.Core.Notifications;

namespace Umbraco.Community.ContentChecks.Core.Policies;

/// <summary>
/// Published before a <see cref="ContentCheckPolicy"/> is saved (cancelable).
/// </summary>
public sealed class ContentCheckPolicySavingNotification : ContentCheckEntitySavingNotification<ContentCheckPolicy>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentCheckPolicySavingNotification"/> class.
    /// </summary>
    /// <param name="entity">The policy being saved.</param>
    /// <param name="messages">Event messages for cancellation reasons.</param>
    public ContentCheckPolicySavingNotification(ContentCheckPolicy entity, EventMessages messages)
        : base(entity, messages)
    {
    }
}
