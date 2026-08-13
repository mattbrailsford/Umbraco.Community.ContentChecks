using Umbraco.Cms.Core.Events;
using Umbraco.Community.ContentChecks.Core.Notifications;

namespace Umbraco.Community.ContentChecks.Core.Policies;

/// <summary>
/// Published after an <see cref="Guard"/> is deleted.
/// </summary>
public sealed class GuardDeletedNotification : ContentCheckEntityDeletedNotification<Guard>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GuardDeletedNotification"/> class.
    /// </summary>
    /// <param name="entityKey">The key of the guard that was deleted.</param>
    /// <param name="messages">Event messages.</param>
    public GuardDeletedNotification(Guid entityKey, EventMessages messages)
        : base(entityKey, messages)
    {
    }
}
