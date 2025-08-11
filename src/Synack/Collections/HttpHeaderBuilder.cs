using System.Diagnostics.CodeAnalysis;

namespace Synack.Collections;

/// <summary>
/// Builder for HTTP header fields - case-insensitive keys, optional trimming helpers.
/// </summary>
/// <remarks>
/// Thread safety: instances of this type are <b>not thread-safe</b>.
/// Use a separate instance per request and do not share across threads.
/// </remarks>
[ExcludeFromCodeCoverage]
internal sealed class HttpHeaderBuilder : MultiMapBuilder
{
    public HttpHeaderBuilder()
        : base(StringComparer.OrdinalIgnoreCase, 16) { }

    public void AddTrimmed(string key, string value)
        => Add(key, value?.Trim() ?? string.Empty);

    public RequestHeaders Build(bool sortKeys = true) => new RequestHeaders(Inner);
}
