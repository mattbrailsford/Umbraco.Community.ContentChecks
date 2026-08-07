using System.Threading.Channels;

namespace Umbraco.Community.ContentChecks.Web.Runs;

/// <summary>
/// Default implementation of <see cref="IContentCheckRunQueue"/>, backed by an unbounded
/// <see cref="Channel{T}"/> — .NET's own documented "queued background tasks" building block.
/// Carries only a run id, not a closure over services: the background worker already knows what
/// to do with a run id (look it up via <see cref="IContentCheckRunTracker"/> and evaluate its
/// content keys), so there's nothing here that could accidentally capture a request-scoped
/// service by mistake.
/// </summary>
internal sealed class ContentCheckRunQueue : IContentCheckRunQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>();

    /// <inheritdoc />
    public void Enqueue(Guid runId) => _channel.Writer.TryWrite(runId);

    /// <inheritdoc />
    public Task<Guid> DequeueAsync(CancellationToken cancellationToken)
        => _channel.Reader.ReadAsync(cancellationToken).AsTask();
}
