using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Umbraco.Community.ContentChecks.Core.Checks;

/// <summary>
/// Computes an <see cref="IContentCheckSnapshot.ContentStamp"/> for snapshots that hold their own
/// values rather than wrapping a persisted content item with an update date to key off.
/// <para>
/// One shared helper rather than a hand-rolled hash per implementation: the two callers
/// (<see cref="InMemoryContentCheckSnapshot"/> and the nested-element snapshot) have to agree on
/// ordering and on variant handling to be correct, and two copies of that agreement drift.
/// </para>
/// </summary>
internal static class ContentCheckValueStamp
{
    /// <summary>
    /// Separates the parts of one value's line. A control character no property alias, culture or
    /// segment can contain, so alias <c>"ab"</c> + culture <c>"c"</c> can't hash the same as alias
    /// <c>"a"</c> + culture <c>"bc"</c> — a bare concatenation would let them.
    /// </summary>
    private const char FieldSeparator = '\u001f';

    /// <summary>
    /// Separates one value's line from the next, for the same reason.
    /// </summary>
    private const char RecordSeparator = '\u001e';

    /// <summary>
    /// Stands in for a null value, distinguishing it from an empty string — "property cleared" and
    /// "property set to empty" reach a check differently, so they must stamp differently.
    /// </summary>
    private const string NullSentinel = "\u0000null";

    /// <summary>
    /// Hashes a snapshot's values into a stamp that changes exactly when those values do.
    /// </summary>
    /// <param name="values">
    /// Every value the snapshot holds. Order doesn't matter — the lines are sorted before hashing,
    /// so two snapshots with the same content stamp the same however they enumerate.
    /// </param>
    /// <returns>A hex-encoded SHA-256 digest.</returns>
    public static string Compute(IEnumerable<(string Alias, string? Culture, string? Segment, object? Value)> values)
    {
        // Culture and segment are part of a value's identity, not decoration: two snapshots
        // differing only in which culture a value belongs to are genuinely different content and
        // must not share a cached result.
        var lines = values
            .Select(v => string.Join(
                FieldSeparator,
                v.Alias,
                v.Culture ?? NullSentinel,
                v.Segment ?? NullSentinel,
                Format(v.Value)))
            .OrderBy(line => line, StringComparer.Ordinal);

        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(string.Join(RecordSeparator, lines)));

        return Convert.ToHexStringLower(digest);
    }

    /// <summary>
    /// Renders one value for hashing, invariantly so a stamp doesn't move with the ambient culture.
    /// </summary>
    /// <param name="value">The value to render.</param>
    /// <returns>Its stable string form.</returns>
    private static string Format(object? value) => value switch
    {
        null => NullSentinel,
        string s => s,
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty,
    };
}
