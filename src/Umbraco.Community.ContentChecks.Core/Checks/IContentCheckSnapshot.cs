namespace Umbraco.Community.ContentChecks.Core.Checks;

/// <summary>
/// A resolved snapshot of exactly one thing — a root document or one nested element — that a
/// check evaluates. Mirrors Umbraco's own <c>IContentBase.GetValue&lt;TValue&gt;(alias, culture,
/// segment)</c>/<c>HasProperty(alias)</c> shape exactly, so an adapter over a real
/// <c>IContent</c> is a one-line delegation, not a translation layer.
/// </summary>
public interface IContentCheckSnapshot
{
    /// <summary>
    /// The content item's durable identity. Never the alias of anything — aliases are
    /// renamable labels, not identity.
    /// </summary>
    Guid Key { get; }

    /// <summary>
    /// The content type alias of this snapshot. A check thinks in alias terms and only ever
    /// reads it during one evaluation, never stores it, so there's nothing to go stale even
    /// though the underlying content type's alias can be renamed.
    /// </summary>
    string ContentTypeAlias { get; }

    /// <summary>
    /// The persisted version identifier of this snapshot, or <c>null</c> when it has no
    /// persisted version yet (unsaved editor state, a Draft Set's in-memory pending state). The
    /// result cache keys on this when present, falling back to hashing the snapshot's values
    /// when it's <c>null</c>.
    /// </summary>
    int? VersionId { get; }

    /// <summary>
    /// Whether this snapshot has the given property. Distinct from <see cref="GetValue{TValue}"/>
    /// returning a default value — resolves "no such property" (→ <see cref="ContentCheckStatus.NotApplicable"/>)
    /// from "empty value" (→ <see cref="ContentCheckStatus.Fail"/>).
    /// </summary>
    /// <param name="alias">The property alias.</param>
    /// <returns><c>true</c> if the property exists on this snapshot.</returns>
    bool HasProperty(string alias);

    /// <summary>
    /// Reads a property's value.
    /// </summary>
    /// <typeparam name="TValue">The value type to read as.</typeparam>
    /// <param name="alias">The property alias.</param>
    /// <param name="culture">The culture to read, or <c>null</c> for an invariant property.</param>
    /// <param name="segment">The segment to read, or <c>null</c> for an unsegmented property.</param>
    /// <returns>The property's value, or <c>default</c> if it has none.</returns>
    TValue? GetValue<TValue>(string alias, string? culture = null, string? segment = null);
}
