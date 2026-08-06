namespace Umbraco.Community.ContentChecks.Core.Policies;

/// <summary>
/// One row of an Effective Checks computation for a content type — a policy that governs it,
/// plus how (<see cref="Provenance"/>). A closed set of three shapes, not one shape with optional
/// fields gated by an enum — <see cref="Composition"/> and <see cref="Cascading"/> each carry only
/// the fields that are actually meaningful for them, so there's nothing to set inconsistently or
/// forget to set. The private base constructor closes the hierarchy to these three; nothing
/// outside this file can add a fourth.
/// </summary>
public abstract class ContentCheckEffectiveCheck
{
    private ContentCheckEffectiveCheck(ContentCheckPolicy policy)
    {
        Policy = policy;
    }

    /// <summary>
    /// The policy that governs the type.
    /// </summary>
    public ContentCheckPolicy Policy { get; }

    /// <summary>
    /// How the policy reaches this type.
    /// </summary>
    public abstract ContentCheckEffectiveCheckProvenance Provenance { get; }

    /// <summary>
    /// The policy targets this type exactly.
    /// </summary>
    public sealed class Direct : ContentCheckEffectiveCheck
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Direct"/> class.
        /// </summary>
        /// <param name="policy">The policy that governs the type.</param>
        public Direct(ContentCheckPolicy policy)
            : base(policy)
        {
        }

        /// <inheritdoc />
        public override ContentCheckEffectiveCheckProvenance Provenance => ContentCheckEffectiveCheckProvenance.Direct;
    }

    /// <summary>
    /// The policy targets a type this one composes from, directly or transitively.
    /// </summary>
    public sealed class Composition : ContentCheckEffectiveCheck
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Composition"/> class.
        /// </summary>
        /// <param name="policy">The policy that governs the type.</param>
        /// <param name="viaContentTypeKey">The composition type the policy actually targets.</param>
        public Composition(ContentCheckPolicy policy, Guid viaContentTypeKey)
            : base(policy)
        {
            ViaContentTypeKey = viaContentTypeKey;
        }

        /// <summary>
        /// The composition type the policy actually targets.
        /// </summary>
        public Guid ViaContentTypeKey { get; }

        /// <inheritdoc />
        public override ContentCheckEffectiveCheckProvenance Provenance => ContentCheckEffectiveCheckProvenance.Composition;
    }

    /// <summary>
    /// The policy targets an element type discoverable inside this type's own block structure.
    /// </summary>
    public sealed class Cascading : ContentCheckEffectiveCheck
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Cascading"/> class.
        /// </summary>
        /// <param name="policy">The policy that governs the type.</param>
        /// <param name="viaPropertyAlias">The alias of the property this type reaches the nested element type through.</param>
        /// <param name="viaElementTypeKey">The nested element type the policy actually targets.</param>
        public Cascading(ContentCheckPolicy policy, string viaPropertyAlias, Guid viaElementTypeKey)
            : base(policy)
        {
            ViaPropertyAlias = viaPropertyAlias;
            ViaElementTypeKey = viaElementTypeKey;
        }

        /// <summary>
        /// The alias of the property this type reaches the nested element type through.
        /// </summary>
        public string ViaPropertyAlias { get; }

        /// <summary>
        /// The nested element type the policy actually targets.
        /// </summary>
        public Guid ViaElementTypeKey { get; }

        /// <inheritdoc />
        public override ContentCheckEffectiveCheckProvenance Provenance => ContentCheckEffectiveCheckProvenance.Cascading;
    }
}
