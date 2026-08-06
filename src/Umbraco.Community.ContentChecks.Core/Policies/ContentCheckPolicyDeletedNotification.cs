using Umbraco.Cms.Core.Events;
using Umbraco.Community.ContentChecks.Core.Notifications;

namespace Umbraco.Community.ContentChecks.Core.Policies;

/// <summary>
/// Published after a <see cref="ContentCheckPolicy"/> is deleted.
/// </summary>
public sealed class ContentCheckPolicyDeletedNotification : ContentCheckEntityDeletedNotification<ContentCheckPolicy>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentCheckPolicyDeletedNotification"/> class.
    /// </summary>
    /// <param name="entityKey">The key of the policy that was deleted.</param>
    /// <param name="messages">Event messages.</param>
    public ContentCheckPolicyDeletedNotification(Guid entityKey, EventMessages messages)
        : base(entityKey, messages)
    {
    }
}
