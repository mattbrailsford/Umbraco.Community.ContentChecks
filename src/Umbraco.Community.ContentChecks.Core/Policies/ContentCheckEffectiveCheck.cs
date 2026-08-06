namespace Umbraco.Community.ContentChecks.Core.Policies;

/// <summary>
/// One row of an Effective Checks computation for a content type — a policy that governs it,
/// plus how (see <see cref="Provenance"/>).
/// </summary>
public sealed class ContentCheckEffectiveCheck
{
    /// <summary>
    /// The policy that governs the type.
    /// </summary>
    public required ContentCheckPolicy Policy { get; init; }

    /// <summary>
    /// How the policy reaches this type.
    /// </summary>
    public required ContentCheckEffectiveCheckProvenance Provenance { get; init; }

    /// <summary>
    /// The composition type the policy actually targets, when <see cref="Provenance"/> is
    /// <see cref="ContentCheckEffectiveCheckProvenance.Composition"/>.
    /// </summary>
    public Guid? ViaContentTypeKey { get; init; }

    /// <summary>
    /// The alias of the property this type reaches the nested element type through, when
    /// <see cref="Provenance"/> is <see cref="ContentCheckEffectiveCheckProvenance.Cascading"/>.
    /// </summary>
    public string? ViaPropertyAlias { get; init; }

    /// <summary>
    /// The nested element type the policy actually targets, when <see cref="Provenance"/> is
    /// <see cref="ContentCheckEffectiveCheckProvenance.Cascading"/>.
    /// </summary>
    public Guid? ViaElementTypeKey { get; init; }
}
