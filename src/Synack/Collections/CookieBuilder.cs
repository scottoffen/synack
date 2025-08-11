using System.Diagnostics.CodeAnalysis;

namespace Synack.Collections;

/// <summary>
/// Builder for cookies - case-sensitive cookie names.
/// </summary>
/// <remarks>
/// Thread safety: instances of this type are <b>not thread-safe</b>.
/// Use a separate instance per request and do not share across threads.
/// </remarks>
[ExcludeFromCodeCoverage]
internal sealed class CookieBuilder : MultiMapBuilder
{
    public CookieBuilder()
        : base(StringComparer.Ordinal, 8) { }

    public ReadOnlyCookies Build(bool sortKeys = false) => new ReadOnlyCookies(Inner);
}
