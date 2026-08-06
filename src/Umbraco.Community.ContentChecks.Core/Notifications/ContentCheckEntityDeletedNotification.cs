using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.Community.ContentChecks.Core.Notifications;

/// <summary>
/// Base class for all entity deleted notifications (not cancelable, published after delete).
/// </summary>
/// <typeparam name="T">The entity type that was deleted.</typeparam>
public abstract class ContentCheckEntityDeletedNotification<T> : StatefulNotification
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentCheckEntityDeletedNotification{T}"/> class.
    /// </summary>
    /// <param name="entityKey">The key of the entity that was deleted.</param>
    /// <param name="messages">The event messages collection.</param>
    protected ContentCheckEntityDeletedNotification(Guid entityKey, EventMessages messages)
    {
        EntityKey = entityKey;
        Messages = messages;
    }

    /// <summary>
    /// Gets the key of the entity that was deleted.
    /// </summary>
    public Guid EntityKey { get; }

    /// <summary>
    /// Gets the event messages collection.
    /// </summary>
    public EventMessages Messages { get; }
}
