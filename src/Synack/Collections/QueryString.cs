using System.Diagnostics.CodeAnalysis;

namespace Synack.Collections;

/// <summary>
/// An immutable, allocation-minimal snapshot of query string parameters.
/// </summary>
/// <remarks>
/// Keys are compared using <see cref="StringComparer.Ordinal"/> for case-sensitive lookups,
/// and the original insertion order is preserved for enumeration.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class QueryString : ReadOnlyMultiMap
{
    public QueryString(Dictionary<string, List<string>> source)
        : base(source, StringComparer.Ordinal) { }
}
