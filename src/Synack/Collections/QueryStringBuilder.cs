using System.Diagnostics.CodeAnalysis;

namespace Synack.Collections;

/// <summary>
/// Builder for query string parameters - case-sensitive keys.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class QueryStringBuilder : MultiMapBuilder
{
    public QueryStringBuilder()
        : base(StringComparer.Ordinal, 8) { }

    public ReadOnlyQueryString Build(bool sortKeys = false) => new ReadOnlyQueryString(Inner);
}
