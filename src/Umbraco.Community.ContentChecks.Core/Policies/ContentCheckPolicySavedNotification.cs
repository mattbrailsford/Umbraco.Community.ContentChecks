using Umbraco.Cms.Core.Events;
using Umbraco.Community.ContentChecks.Core.Notifications;

namespace Umbraco.Community.ContentChecks.Core.Policies;

/// <summary>
/// Published after a <see cref="ContentCheckPolicy"/> is saved.
/// </summary>
public sealed class ContentCheckPolicySavedNotification : ContentCheckEntitySavedNotification<ContentCheckPolicy>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentCheckPolicySavedNotification"/> class.
    /// </summary>
    /// <param name="entity">The policy that was saved.</param>
    /// <param name="messages">Event messages.</param>
    public ContentCheckPolicySavedNotification(ContentCheckPolicy entity, EventMessages messages)
        : base(entity, messages)
    {
    }
}
