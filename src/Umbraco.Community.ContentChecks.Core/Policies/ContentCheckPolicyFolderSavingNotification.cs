using Umbraco.Cms.Core.Events;
using Umbraco.Community.ContentChecks.Core.Notifications;

namespace Umbraco.Community.ContentChecks.Core.Policies;

/// <summary>
/// Published before a <see cref="ContentCheckPolicyFolder"/> is saved (cancelable).
/// </summary>
public sealed class ContentCheckPolicyFolderSavingNotification : ContentCheckEntitySavingNotification<ContentCheckPolicyFolder>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentCheckPolicyFolderSavingNotification"/> class.
    /// </summary>
    /// <param name="entity">The folder being saved.</param>
    /// <param name="messages">Event messages for cancellation reasons.</param>
    public ContentCheckPolicyFolderSavingNotification(ContentCheckPolicyFolder entity, EventMessages messages)
        : base(entity, messages)
    {
    }
}
