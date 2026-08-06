using Umbraco.Cms.Core.Events;
using Umbraco.Community.ContentChecks.Core.Notifications;

namespace Umbraco.Community.ContentChecks.Core.Policies;

/// <summary>
/// Published after a <see cref="ContentCheckPolicyFolder"/> is deleted.
/// </summary>
public sealed class ContentCheckPolicyFolderDeletedNotification : ContentCheckEntityDeletedNotification<ContentCheckPolicyFolder>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentCheckPolicyFolderDeletedNotification"/> class.
    /// </summary>
    /// <param name="entityKey">The key of the folder that was deleted.</param>
    /// <param name="messages">Event messages.</param>
    public ContentCheckPolicyFolderDeletedNotification(Guid entityKey, EventMessages messages)
        : base(entityKey, messages)
    {
    }
}
