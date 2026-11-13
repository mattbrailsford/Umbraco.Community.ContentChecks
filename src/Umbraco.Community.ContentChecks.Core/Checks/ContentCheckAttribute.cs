namespace Umbraco.Community.ContentChecks.Core.Checks;

/// <summary>
/// Attribute to mark <see cref="IContentCheck"/> implementations, declaring their identity and
/// classification. Every real check is declared this way and inherits
/// <see cref="PropertyContentCheckBase{TConfig}"/> or <see cref="Markup.MarkupContentCheckBase{TConfig}"/>
/// rather than implementing <see cref="IContentCheck"/> directly.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class ContentCheckAttribute(string alias, string name) : Attribute
{
    /// <summary>
    /// The unique, code-registered identifier of the check.
    /// </summary>
    public string Alias { get; } = alias;

    /// <summary>
    /// The display name of the check.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Whether running the check is metered. Defaults to <see cref="ContentCheckCost.Free"/>.
    /// </summary>
    public ContentCheckCost Cost { get; set; } = ContentCheckCost.Free;

    /// <summary>
    /// Whether the check's output is reproducible. Defaults to <see cref="ContentCheckKind.Deterministic"/>.
    /// </summary>
    public ContentCheckKind Kind { get; set; } = ContentCheckKind.Deterministic;
}
