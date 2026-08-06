namespace Umbraco.Community.ContentChecks.Core.Checks;

/// <summary>
/// Whether a check's output is reproducible for the same input, independent of whether running
/// it costs money (see <see cref="ContentCheckCost"/>). This is what the result cache and the
/// UI's trust disclosure key off.
/// </summary>
public enum ContentCheckKind
{
    /// <summary>
    /// The same input always produces the same result (e.g. a required-property check).
    /// </summary>
    Deterministic,

    /// <summary>
    /// The result can vary run-to-run for the same input (an AI judge, a flaky third-party
    /// service).
    /// </summary>
    Nondeterministic,
}
