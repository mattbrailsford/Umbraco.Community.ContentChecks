using Umbraco.Cms.Core.Events;
using Umbraco.Community.ContentChecks.Core.Notifications;

namespace Umbraco.Community.ContentChecks.Core.Policies;

/// <summary>
/// Published before an <see cref="Guard"/> is saved (cancelable).
/// </summary>
public sealed class GuardSavingNotification : ContentCheckEntitySavingNotification<Guard>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GuardSavingNotification"/> class.
    /// </summary>
    /// <param name="entity">The guard being saved.</param>
    /// <param name="messages">Event messages for cancellation reasons.</param>
    public GuardSavingNotification(Guard entity, EventMessages messages)
        : base(entity, messages)
    {
    }
}
