namespace Umbraco.Community.ContentChecks.Core.Policies;

/// <summary>
/// Computes which check policies govern a content type — schema/design time, no live content
/// needed. The set can change because of an edit somewhere completely unrelated to any check
/// policy (a composition added to a doc type, a new allowed block on a Block List config), which
/// is exactly why this needs to be computable and visible rather than left implicit.
/// </summary>
public interface IContentCheckEffectiveChecksService
{
    /// <summary>
    /// Computes every policy that governs the given content type, with provenance.
    /// </summary>
    /// <param name="contentTypeKey">The content type (document type or element type) to compute for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Every policy that governs the type, with provenance.</returns>
    Task<IReadOnlyList<ContentCheckEffectiveCheck>> GetEffectiveChecksAsync(
        Guid contentTypeKey, CancellationToken cancellationToken = default);
}
