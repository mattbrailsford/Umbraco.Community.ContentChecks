using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.Community.ContentChecks.Core.Notifications;

/// <summary>
/// Base class for all entity saving notifications (cancelable, published before save).
/// </summary>
/// <typeparam name="T">The entity type being saved.</typeparam>
public abstract class ContentCheckEntitySavingNotification<T> : CancelableNotification
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentCheckEntitySavingNotification{T}"/> class.
    /// </summary>
    /// <param name="entity">The entity being saved.</param>
    /// <param name="messages">The event messages collection.</param>
    protected ContentCheckEntitySavingNotification(T entity, EventMessages messages)
        : base(messages)
    {
        Entity = entity;
    }

    /// <summary>
    /// Gets the entity being saved.
    /// </summary>
    public T Entity { get; }
}
