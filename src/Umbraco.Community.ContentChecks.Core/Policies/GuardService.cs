using Umbraco.Cms.Core.Events;

namespace Umbraco.Community.ContentChecks.Core.Policies;

/// <summary>
/// Default implementation of <see cref="IGuardService"/>. Repository access pattern:
/// nothing outside this service touches <see cref="IGuardRepository"/> directly, so
/// nothing outside this service is a second place the CRUD notifications could fire from. Mirrors
/// <see cref="ContentCheckPolicyService"/>'s role for policies.
/// </summary>
internal sealed class GuardService : IGuardService
{
    private readonly IGuardRepository _repository;
    private readonly IEventAggregator _eventAggregator;

    /// <summary>
    /// Initializes a new instance of the <see cref="GuardService"/> class.
    /// </summary>
    /// <param name="repository">The guard repository.</param>
    /// <param name="eventAggregator">Publishes the CRUD notifications.</param>
    public GuardService(IGuardRepository repository, IEventAggregator eventAggregator)
    {
        _repository = repository;
        _eventAggregator = eventAggregator;
    }

    /// <inheritdoc />
    public Task<Guard?> GetByKeyAsync(Guid key, CancellationToken cancellationToken = default)
        => _repository.GetByKeyAsync(key, cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<Guard>> GetAllAsync(CancellationToken cancellationToken = default)
        => _repository.GetAllAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<Guard> SaveAsync(Guard guard, CancellationToken cancellationToken = default)
    {
        var messages = new EventMessages();
        var savingNotification = new GuardSavingNotification(guard, messages);
        await _eventAggregator.PublishAsync(savingNotification, cancellationToken);
        if (savingNotification.Cancel)
        {
            throw new InvalidOperationException($"Save of guard '{guard.Name}' was cancelled.");
        }

        var saved = await _repository.SaveAsync(guard, cancellationToken);

        var savedNotification = new GuardSavedNotification(saved, messages);
        savedNotification.State = savingNotification.State;
        await _eventAggregator.PublishAsync(savedNotification, cancellationToken);

        return saved;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid key, CancellationToken cancellationToken = default)
    {
        var messages = new EventMessages();
        var deletingNotification = new GuardDeletingNotification(key, messages);
        await _eventAggregator.PublishAsync(deletingNotification, cancellationToken);
        if (deletingNotification.Cancel)
        {
            return false;
        }

        var deleted = await _repository.DeleteAsync(key, cancellationToken);
        if (!deleted)
        {
            return false;
        }

        var deletedNotification = new GuardDeletedNotification(key, messages);
        deletedNotification.State = deletingNotification.State;
        await _eventAggregator.PublishAsync(deletedNotification, cancellationToken);

        return true;
    }
}
