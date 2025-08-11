using System.Diagnostics.CodeAnalysis;

namespace Synack.Collections;

/// <summary>
/// Builder for cookies - case-sensitive cookie names.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class CookieBuilder : MultiMapBuilder
{
    public CookieBuilder()
        : base(StringComparer.Ordinal, 8) { }

    public ReadOnlyCookies Build(bool sortKeys = false) => new ReadOnlyCookies(Inner);
}
