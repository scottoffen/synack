using System.Net;
using Synack.Collections;
using Synack.Exceptions;

namespace Synack.Extensions;

internal static class QueryStringExtensions
{
    public static QueryString MapToQueryString(this string? query, RequestParsingLimits? limits)
    {
        var builder = new QueryStringBuilder();

        if (string.IsNullOrEmpty(query))
            return builder.Build();

        var start = query[0] == '?' ? 1 : 0;
        if (start >= query.Length)
            return builder.Build();

        // Rough distinct-key estimate: (# of '&') + 1
        var estimatedKeys = 1;
        for (var i = start; i < query.Length; i++)
            if (query[i] == '&') estimatedKeys++;
        builder.EnsureCapacity(estimatedKeys);

        var distinctKeys = 0;

        var iPos = start;
        while (true)
        {
            if (iPos >= query.Length)
                break;

            var segStart = iPos;
            var segEnd = segStart;
            while (segEnd < query.Length && query[segEnd] != '&') segEnd++;

            if (segEnd > segStart)
            {
                var eq = -1;
                for (var k = segStart; k < segEnd; k++)
                {
                    if (query[k] == '=')
                    {
                        eq = k;
                        break;
                    }
                }

                var keyStart = segStart;
                var keyLen = (eq >= 0 ? eq : segEnd) - segStart;
                var valStart = (eq >= 0 ? eq + 1 : segEnd);
                var valLen = segEnd - valStart;

                var key = DecodeQueryComponent(query, keyStart, keyLen);
                var val = DecodeQueryComponent(query, valStart, valLen);

                if (!builder.TryGetValues(key, out var existing))
                {
                    distinctKeys++;
                    if (limits?.MaxQueryParameterCount is int maxParams && distinctKeys > maxParams)
                        throw new RequestLimitExceededException("MaxQueryParameterCount", maxParams, distinctKeys);
                }
                else if (limits?.MaxQueryValuesPerKey is int maxPerKey && existing!.Count + 1 > maxPerKey)
                {
                    throw new RequestLimitExceededException("MaxQueryValuesPerKey", maxPerKey, existing.Count + 1);
                }

                builder.Add(key, val);
            }

            if (segEnd == query.Length) break;
            iPos = segEnd + 1;
        }

        return builder.Build();
    }

    private static string DecodeQueryComponent(string s, int start, int length)
    {
        if (length <= 0) return string.Empty;

        var end = start + length;
        var needsDecoding = false;
        for (var i = start; i < end; i++)
        {
            var c = s[i];
            if (c == '%' || c == '+') { needsDecoding = true; break; }
        }

        if (!needsDecoding)
            return s.Substring(start, length);

        // WebUtility.UrlDecode handles %xx decoding and treats '+' as space for query semantics.
        // To avoid allocating the whole substring when possible, we pay for one substring here.
        return WebUtility.UrlDecode(s.Substring(start, length));
    }
}
