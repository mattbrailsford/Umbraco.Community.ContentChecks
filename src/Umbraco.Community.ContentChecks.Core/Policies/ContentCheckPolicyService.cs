using Umbraco.Cms.Core.Events;

namespace Umbraco.Community.ContentChecks.Core.Policies;

/// <summary>
/// Default implementation of <see cref="IContentCheckPolicyService"/>. Repository access pattern:
/// nothing outside this service touches <see cref="IContentCheckPolicyRepository"/> directly, so
/// nothing outside this service is a second place the CRUD notifications could fire from.
/// </summary>
internal sealed class ContentCheckPolicyService : IContentCheckPolicyService
{
    private readonly IContentCheckPolicyRepository _repository;
    private readonly IEventAggregator _eventAggregator;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentCheckPolicyService"/> class.
    /// </summary>
    /// <param name="repository">The policy/folder repository.</param>
    /// <param name="eventAggregator">Publishes the CRUD notifications.</param>
    public ContentCheckPolicyService(IContentCheckPolicyRepository repository, IEventAggregator eventAggregator)
    {
        _repository = repository;
        _eventAggregator = eventAggregator;
    }

    /// <inheritdoc />
    public Task<ContentCheckPolicy?> GetPolicyAsync(Guid key, CancellationToken cancellationToken = default)
        => _repository.GetByKeyAsync(key, cancellationToken);

    /// <inheritdoc />
    public Task<ContentCheckPolicy?> GetPolicyByAliasAsync(string alias, CancellationToken cancellationToken = default)
        => _repository.GetByAliasAsync(alias, cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<ContentCheckPolicy>> GetAllPoliciesAsync(CancellationToken cancellationToken = default)
        => _repository.GetAllAsync(cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<ContentCheckPolicy>> GetPoliciesByContentTypeKeysAsync(IReadOnlyCollection<Guid> contentTypeKeys, CancellationToken cancellationToken = default)
        => _repository.GetByContentTypeKeysAsync(contentTypeKeys, cancellationToken);

    /// <inheritdoc />
    public async Task<ContentCheckPolicy> SavePolicyAsync(ContentCheckPolicy policy, CancellationToken cancellationToken = default)
    {
        var existingByAlias = await _repository.GetByAliasAsync(policy.Alias, cancellationToken);
        if (existingByAlias is not null && existingByAlias.Key != policy.Key)
        {
            throw new InvalidOperationException($"A policy with alias '{policy.Alias}' already exists.");
        }

        var messages = new EventMessages();
        var savingNotification = new ContentCheckPolicySavingNotification(policy, messages);
        await _eventAggregator.PublishAsync(savingNotification, cancellationToken);
        if (savingNotification.Cancel)
        {
            throw new InvalidOperationException($"Save of policy '{policy.Alias}' was cancelled.");
        }

        var saved = await _repository.SaveAsync(policy, cancellationToken);

        var savedNotification = new ContentCheckPolicySavedNotification(saved, messages);
        savedNotification.State = savingNotification.State;
        await _eventAggregator.PublishAsync(savedNotification, cancellationToken);

        return saved;
    }

    /// <inheritdoc />
    public async Task<bool> DeletePolicyAsync(Guid key, CancellationToken cancellationToken = default)
    {
        var messages = new EventMessages();
        var deletingNotification = new ContentCheckPolicyDeletingNotification(key, messages);
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

        var deletedNotification = new ContentCheckPolicyDeletedNotification(key, messages);
        deletedNotification.State = deletingNotification.State;
        await _eventAggregator.PublishAsync(deletedNotification, cancellationToken);

        return true;
    }

    /// <inheritdoc />
    public Task<ContentCheckPolicyFolder?> GetFolderAsync(Guid key, CancellationToken cancellationToken = default)
        => _repository.GetFolderByKeyAsync(key, cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<ContentCheckPolicyFolder>> GetAllFoldersAsync(CancellationToken cancellationToken = default)
        => _repository.GetAllFoldersAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<ContentCheckPolicyFolder> SaveFolderAsync(ContentCheckPolicyFolder folder, CancellationToken cancellationToken = default)
    {
        var messages = new EventMessages();
        var savingNotification = new ContentCheckPolicyFolderSavingNotification(folder, messages);
        await _eventAggregator.PublishAsync(savingNotification, cancellationToken);
        if (savingNotification.Cancel)
        {
            throw new InvalidOperationException($"Save of folder '{folder.Name}' was cancelled.");
        }

        var saved = await _repository.SaveFolderAsync(folder, cancellationToken);

        var savedNotification = new ContentCheckPolicyFolderSavedNotification(saved, messages);
        savedNotification.State = savingNotification.State;
        await _eventAggregator.PublishAsync(savedNotification, cancellationToken);

        return saved;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteFolderAsync(Guid key, CancellationToken cancellationToken = default)
    {
        if (await FolderHasChildrenAsync(key, cancellationToken))
        {
            throw new InvalidOperationException("Cannot delete a folder that still contains policies or subfolders. Empty it first.");
        }

        var messages = new EventMessages();
        var deletingNotification = new ContentCheckPolicyFolderDeletingNotification(key, messages);
        await _eventAggregator.PublishAsync(deletingNotification, cancellationToken);
        if (deletingNotification.Cancel)
        {
            return false;
        }

        var deleted = await _repository.DeleteFolderAsync(key, cancellationToken);
        if (!deleted)
        {
            return false;
        }

        var deletedNotification = new ContentCheckPolicyFolderDeletedNotification(key, messages);
        deletedNotification.State = deletingNotification.State;
        await _eventAggregator.PublishAsync(deletedNotification, cancellationToken);

        return true;
    }

    /// <summary>
    /// Blocks deleting a non-empty folder, rather than cascading — a check policy governs real
    /// publish-blocking behavior, so silently deleting several blocking-mode policies because
    /// their containing folder was removed is a bigger footgun here than it would be for a less
    /// consequential entity.
    /// </summary>
    private async Task<bool> FolderHasChildrenAsync(Guid key, CancellationToken cancellationToken)
        => await _repository.PolicyExistsInFolderAsync(key, cancellationToken)
            || await _repository.FolderExistsInFolderAsync(key, cancellationToken);
}
