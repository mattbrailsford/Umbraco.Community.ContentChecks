namespace Umbraco.Community.ContentChecks.Core.Markup;

/// <summary>
/// Mints a credential that puts a page-fetch request into Umbraco preview mode for a given
/// backoffice user, so a markup check's default fetch sees the current draft rather than the
/// published version (docs/markup-checks-exploration.md §7).
/// </summary>
/// <remarks>
/// Deliberately its own small seam, not folded directly into the (not yet built) markup
/// provider: the real mechanism changes between Umbraco majors -- see
/// <see cref="PreviewTokenCredentialMinter"/>'s remarks for v18's, and
/// docs/markup-checks-exploration.md §9.4 for why v19's is different and heavier. Swapping this
/// for a v19 implementation on that branch should mean replacing one registration, not
/// redesigning whatever calls it.
/// </remarks>
public interface IContentCheckPreviewCredentialMinter
{
    /// <summary>
    /// Mints a preview credential for the given backoffice user.
    /// </summary>
    /// <param name="judgeUserKey">
    /// The backoffice user acting as "the judge" -- whoever's preview rights the fetch borrows.
    /// Should be a user with access to every start node a markup check might ever fetch (an
    /// Admin-group service account, in practice) -- a user without rights at a given node has
    /// their preview request silently served the published version instead of the draft.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The credential, or <c>null</c> if one couldn't be minted (e.g. the user doesn't exist).
    /// </returns>
    Task<ContentCheckPreviewCredential?> MintAsync(Guid judgeUserKey, CancellationToken cancellationToken);
}
