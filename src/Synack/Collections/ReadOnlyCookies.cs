using System.Diagnostics.CodeAnalysis;

namespace Synack.Collections;

/// <summary>
/// An immutable, allocation-minimal snapshot of HTTP cookies.
/// </summary>
/// <remarks>
/// Cookie names are compared using <see cref="StringComparer.Ordinal"/> for case-sensitive lookups,
/// and the original insertion order is preserved for enumeration.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class ReadOnlyCookies : ReadOnlyMultiMap
{
    public ReadOnlyCookies(Dictionary<string, List<string>> source)
        : base(source, StringComparer.Ordinal) { }
}
