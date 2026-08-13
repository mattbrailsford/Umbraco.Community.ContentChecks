namespace Umbraco.Community.ContentChecks.Core.Checks;

/// <summary>
/// How important a failing check is — set by the rule author. Distinct from
/// <see cref="ContentCheckResponse"/>, which decides what actually happens when a rule at this
/// level fails; that decision belongs to a Guard, not the rule itself. See
/// docs/guard-spec.md.
/// </summary>
public enum RequirementLevel
{
    /// <summary>
    /// A nice-to-have. Safe to ignore.
    /// </summary>
    Optional,

    /// <summary>
    /// Should be fixed, but isn't a hard requirement.
    /// </summary>
    Recommended,

    /// <summary>
    /// Must be fixed.
    /// </summary>
    Mandatory,
}
