using Umbraco.Cms.Core.Events;
using Umbraco.Community.ContentChecks.Core.Notifications;

namespace Umbraco.Community.ContentChecks.Core.Policies;

/// <summary>
/// Published after a <see cref="ContentCheckPolicyFolder"/> is saved.
/// </summary>
public sealed class ContentCheckPolicyFolderSavedNotification : ContentCheckEntitySavedNotification<ContentCheckPolicyFolder>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentCheckPolicyFolderSavedNotification"/> class.
    /// </summary>
    /// <param name="entity">The folder that was saved.</param>
    /// <param name="messages">Event messages.</param>
    public ContentCheckPolicyFolderSavedNotification(ContentCheckPolicyFolder entity, EventMessages messages)
        : base(entity, messages)
    {
    }
}
