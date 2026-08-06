using Umbraco.Cms.Core.Events;
using Umbraco.Community.ContentChecks.Core.Notifications;

namespace Umbraco.Community.ContentChecks.Core.Policies;

/// <summary>
/// Published before a <see cref="ContentCheckPolicyFolder"/> is deleted (cancelable).
/// </summary>
public sealed class ContentCheckPolicyFolderDeletingNotification : ContentCheckEntityDeletingNotification<ContentCheckPolicyFolder>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentCheckPolicyFolderDeletingNotification"/> class.
    /// </summary>
    /// <param name="entityKey">The key of the folder being deleted.</param>
    /// <param name="messages">Event messages for cancellation reasons.</param>
    public ContentCheckPolicyFolderDeletingNotification(Guid entityKey, EventMessages messages)
        : base(entityKey, messages)
    {
    }
}
