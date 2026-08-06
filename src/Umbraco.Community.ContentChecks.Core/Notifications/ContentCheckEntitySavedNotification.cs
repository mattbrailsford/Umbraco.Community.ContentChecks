using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.Community.ContentChecks.Core.Notifications;

/// <summary>
/// Base class for all entity saved notifications (not cancelable, published after save).
/// </summary>
/// <typeparam name="T">The entity type that was saved.</typeparam>
public abstract class ContentCheckEntitySavedNotification<T> : StatefulNotification
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentCheckEntitySavedNotification{T}"/> class.
    /// </summary>
    /// <param name="entity">The entity that was saved.</param>
    /// <param name="messages">The event messages collection.</param>
    protected ContentCheckEntitySavedNotification(T entity, EventMessages messages)
    {
        Entity = entity;
        Messages = messages;
    }

    /// <summary>
    /// Gets the entity that was saved.
    /// </summary>
    public T Entity { get; }

    /// <summary>
    /// Gets the event messages collection.
    /// </summary>
    public EventMessages Messages { get; }
}
