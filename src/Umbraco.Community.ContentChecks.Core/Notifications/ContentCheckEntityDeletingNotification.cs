using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.Community.ContentChecks.Core.Notifications;

/// <summary>
/// Base class for all entity deleting notifications (cancelable, published before delete).
/// </summary>
/// <typeparam name="T">The entity type being deleted.</typeparam>
public abstract class ContentCheckEntityDeletingNotification<T> : CancelableNotification
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentCheckEntityDeletingNotification{T}"/> class.
    /// </summary>
    /// <param name="entityKey">The key of the entity being deleted.</param>
    /// <param name="messages">The event messages collection.</param>
    protected ContentCheckEntityDeletingNotification(Guid entityKey, EventMessages messages)
        : base(messages)
    {
        EntityKey = entityKey;
    }

    /// <summary>
    /// Gets the key of the entity being deleted.
    /// </summary>
    public Guid EntityKey { get; }
}
