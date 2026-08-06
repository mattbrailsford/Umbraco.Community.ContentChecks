using Umbraco.Cms.Core.Events;
using Umbraco.Community.ContentChecks.Core.Notifications;

namespace Umbraco.Community.ContentChecks.Core.Policies;

/// <summary>
/// Published before a <see cref="ContentCheckPolicy"/> is deleted (cancelable).
/// </summary>
public sealed class ContentCheckPolicyDeletingNotification : ContentCheckEntityDeletingNotification<ContentCheckPolicy>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentCheckPolicyDeletingNotification"/> class.
    /// </summary>
    /// <param name="entityKey">The key of the policy being deleted.</param>
    /// <param name="messages">Event messages for cancellation reasons.</param>
    public ContentCheckPolicyDeletingNotification(Guid entityKey, EventMessages messages)
        : base(entityKey, messages)
    {
    }
}
