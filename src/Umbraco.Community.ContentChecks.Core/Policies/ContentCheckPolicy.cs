namespace Umbraco.Community.ContentChecks.Core.Policies;

/// <summary>
/// A named set of check rules attached to exactly one content type (a document type or an element
/// type — Umbraco doesn't distinguish the two at the <c>IContentType</c> level, so neither does
/// this), plus an optional start node. Not a multi-type list: a content type governed by two
/// differently-scoped rule sets (e.g. a composition-wide baseline plus a type-specific refinement)
/// is expressed as two separate <see cref="ContentCheckPolicy"/> records, with no relationship
/// between them required — they combine by simple union at resolution time.
/// </summary>
public sealed class ContentCheckPolicy
{
    /// <summary>
    /// The policy's durable identity.
    /// </summary>
    public Guid Key { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The unique alias of the policy.
    /// </summary>
    public required string Alias { get; set; }

    /// <summary>
    /// The display name of the policy.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// The content type this policy attaches to — its durable Key, not its alias, so a content
    /// type rename never silently detaches the policy.
    /// </summary>
    public required Guid ContentTypeKey { get; set; }

    /// <summary>
    /// Restricts the policy to content underneath this node. <c>null</c> means everywhere the
    /// content type reaches.
    /// </summary>
    public Guid? StartNodeKey { get; set; }

    /// <summary>
    /// The folder this policy is organized under in the backoffice tree. <c>null</c> means at the root.
    /// </summary>
    public Guid? ParentFolderKey { get; set; }

    /// <summary>
    /// The rules this policy runs.
    /// </summary>
    public IReadOnlyList<ContentCheckPolicyRule> Rules { get; set; } = [];
}
