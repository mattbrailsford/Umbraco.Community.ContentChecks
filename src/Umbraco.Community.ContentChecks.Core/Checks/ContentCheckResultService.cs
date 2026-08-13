namespace Umbraco.Community.ContentChecks.Core.Checks;

/// <summary>
/// Default implementation of <see cref="IContentCheckResultService"/>. Repository access pattern:
/// nothing outside this service touches <see cref="IContentCheckResultRepository"/> directly, the
/// same convention <c>GuardService</c>/<c>ContentCheckPolicyService</c> follow for their own
/// repositories — this is what lets the repository stay <c>internal</c>.
/// </summary>
internal sealed class ContentCheckResultService : IContentCheckResultService
{
    private readonly IContentCheckResultRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentCheckResultService"/> class.
    /// </summary>
    /// <param name="repository">The result repository.</param>
    public ContentCheckResultService(IContentCheckResultRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ContentCheckStoredResult>> GetByContentKeyAsync(
        Guid contentKey, string? culture = null, string? segment = null, CancellationToken cancellationToken = default)
        => _repository.GetByContentKeyAsync(contentKey, culture, segment, cancellationToken);

    /// <inheritdoc />
    public Task ReplaceForContentAsync(
        Guid contentKey,
        string? culture,
        string? segment,
        IReadOnlyList<ContentCheckStoredResult> results,
        CancellationToken cancellationToken = default)
        => _repository.ReplaceForContentAsync(contentKey, culture, segment, results, cancellationToken);
}
