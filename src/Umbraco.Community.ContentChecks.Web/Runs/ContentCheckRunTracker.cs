using System.Collections.Concurrent;

namespace Umbraco.Community.ContentChecks.Web.Runs;

/// <summary>
/// Default implementation of <see cref="IContentCheckRunTracker"/>. A singleton wrapping a
/// <see cref="ConcurrentDictionary{TKey,TValue}"/> — the same shape recorded in the Management API
/// &amp; UI spec's "Live-run mechanism".
/// </summary>
internal sealed class ContentCheckRunTracker : IContentCheckRunTracker
{
    private readonly ConcurrentDictionary<Guid, ContentCheckRunState> _runs = new();

    /// <inheritdoc />
    public ContentCheckRunState StartRun(IReadOnlyList<Guid> contentKeys)
    {
        var run = new ContentCheckRunState(Guid.NewGuid(), contentKeys);
        _runs[run.RunId] = run;
        return run;
    }

    /// <inheritdoc />
    public ContentCheckRunState? GetRun(Guid runId) => _runs.GetValueOrDefault(runId);
}
