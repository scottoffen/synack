using System.Diagnostics.CodeAnalysis;

namespace Synack.Collections;

/// <summary>
/// Builder for HTTP header fields - case-insensitive keys, optional trimming helpers.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class HttpHeaderBuilder : MultiMapBuilder
{
    public HttpHeaderBuilder()
        : base(StringComparer.OrdinalIgnoreCase, 16) { }

    public void AddTrimmed(string key, string value)
        => Add(key, value?.Trim() ?? string.Empty);

    public ReadOnlyHttpHeaders Build(bool sortKeys = true) => new ReadOnlyHttpHeaders(Inner);
}
