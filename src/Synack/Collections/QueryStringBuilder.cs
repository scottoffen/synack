using System.Diagnostics.CodeAnalysis;

namespace Synack.Collections;

/// <summary>
/// Builder for query string parameters - case-sensitive keys.
/// </summary>
/// <remarks>
/// Thread safety: instances of this type are <b>not thread-safe</b>.
/// Use a separate instance per request and do not share across threads.
/// </remarks>
[ExcludeFromCodeCoverage]
internal sealed class QueryStringBuilder : MultiMapBuilder
{
    public QueryStringBuilder()
        : base(StringComparer.Ordinal, 8) { }

    public QueryString Build(bool sortKeys = false) => new QueryString(Inner);
}
