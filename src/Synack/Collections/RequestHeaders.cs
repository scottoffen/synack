using System.Diagnostics.CodeAnalysis;

namespace Synack.Collections;

/// <summary>
/// An immutable, allocation-minimal snapshot of HTTP request headers.
/// </summary>
/// <remarks>
/// Keys are compared using <see cref="StringComparer.OrdinalIgnoreCase"/> per RFC 9110,
/// and the collection is sorted by key for deterministic enumeration order.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class RequestHeaders : ReadOnlyMultiMap
{
    public RequestHeaders(Dictionary<string, List<string>> source)
        : base(source, StringComparer.OrdinalIgnoreCase, sortKeys: true) { }
}
