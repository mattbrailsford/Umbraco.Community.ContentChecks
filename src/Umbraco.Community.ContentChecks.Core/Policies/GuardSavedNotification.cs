using Umbraco.Cms.Core.Events;
using Umbraco.Community.ContentChecks.Core.Notifications;

namespace Umbraco.Community.ContentChecks.Core.Policies;

/// <summary>
/// Published after an <see cref="Guard"/> is saved.
/// </summary>
public sealed class GuardSavedNotification : ContentCheckEntitySavedNotification<Guard>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GuardSavedNotification"/> class.
    /// </summary>
    /// <param name="entity">The guard that was saved.</param>
    /// <param name="messages">Event messages.</param>
    public GuardSavedNotification(Guard entity, EventMessages messages)
        : base(entity, messages)
    {
    }
}
