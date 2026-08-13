using Umbraco.Cms.Core.Events;
using Umbraco.Community.ContentChecks.Core.Notifications;

namespace Umbraco.Community.ContentChecks.Core.Policies;

/// <summary>
/// Published before an <see cref="Guard"/> is deleted (cancelable).
/// </summary>
public sealed class GuardDeletingNotification : ContentCheckEntityDeletingNotification<Guard>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GuardDeletingNotification"/> class.
    /// </summary>
    /// <param name="entityKey">The key of the guard being deleted.</param>
    /// <param name="messages">Event messages for cancellation reasons.</param>
    public GuardDeletingNotification(Guid entityKey, EventMessages messages)
        : base(entityKey, messages)
    {
    }
}
