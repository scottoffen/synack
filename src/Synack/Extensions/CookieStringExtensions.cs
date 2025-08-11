using System.Runtime.CompilerServices;
using System.Text;
using Synack.Collections;
using Synack.Exceptions;

namespace Synack.Extensions;

internal static class CookieStringExtensions
{
    public static RequestCookies MapToCookies(this string? header, RequestParsingLimits? limits)
    {
        var builder = new CookieBuilder();
        if (string.IsNullOrEmpty(header)) return builder.Build();

        var estimated = 1;
        for (var i = 0; i < header.Length; i++) if (header[i] == ';') estimated++;
        builder.EnsureCapacity(estimated);

        var bytesByName = new System.Collections.Generic.Dictionary<string, int>(StringComparer.Ordinal);
        var totalBytes = 0;
        var distinct = 0;

        var pos = 0;
        while (NextSegment(header, ref pos, out var segStart, out var segEnd))
        {
            TrimOws(header, ref segStart, ref segEnd);
            if (segEnd <= segStart) continue;

            SplitOnFirstEquals(header, segStart, segEnd, out var ns, out var ne, out var vs, out var ve);
            TrimOws(header, ref ns, ref ne);
            TrimOws(header, ref vs, ref ve);
            if (ne <= ns) continue;

            var name = header.Substring(ns, ne - ns);
            if (vs < ve && header[vs] == '"' && header[ve - 1] == '"' && ve - vs >= 2)
            {
                vs++; ve--;
            }
            var value = (vs < ve) ? header.Substring(vs, ve - vs) : string.Empty;

            // Limits: per-name bytes, total bytes, count (distinct names)
            var pairBytes = Utf8ByteCount(name) + Utf8ByteCount(value);
            if (limits?.MaxCookieBytesPerName is int maxPer && pairBytes > maxPer)
                throw new RequestLimitExceededException("MaxCookieBytesPerName", maxPer, pairBytes);

            if (!bytesByName.TryGetValue(name, out var prevBytes))
            {
                distinct++;
                if (limits?.MaxCookieCount is int maxCount && distinct > maxCount)
                    throw new RequestLimitExceededException("MaxCookieCount", maxCount, distinct);

                bytesByName[name] = pairBytes;
                totalBytes += pairBytes;
            }
            else
            {
                totalBytes += pairBytes - prevBytes;
                bytesByName[name] = pairBytes;
            }

            if (limits?.MaxCookiesBytesTotal is int maxTotal && totalBytes > maxTotal)
                throw new RequestLimitExceededException("MaxCookiesBytesTotal", maxTotal, totalBytes);

            // last-wins
            if (builder.TryGetValues(name, out var list))
            {
                list!.Clear();
                list.Add(value);
            }
            else
            {
                builder.Add(name, value);
            }
        }

        return builder.Build();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool NextSegment(string s, ref int pos, out int start, out int end)
        {
            if (pos >= s.Length) { start = end = 0; return false; }
            start = pos;
            var i = pos;
            while (i < s.Length && s[i] != ';') i++;
            end = i;
            pos = (i < s.Length) ? i + 1 : i;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void SplitOnFirstEquals(string s, int start, int end, out int nameStart, out int nameEnd, out int valueStart, out int valueEnd)
        {
            var eq = -1;
            for (var i = start; i < end; i++) { if (s[i] == '=') { eq = i; break; } }
            if (eq >= 0)
            {
                nameStart = start; nameEnd = eq;
                valueStart = eq + 1; valueEnd = end;
            }
            else
            {
                nameStart = start; nameEnd = end;
                valueStart = end; valueEnd = end;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void TrimOws(string s, ref int start, ref int end)
        {
            while (start < end && (s[start] == ' ' || s[start] == '\t')) start++;
            while (end > start && (s[end - 1] == ' ' || s[end - 1] == '\t')) end--;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int Utf8ByteCount(string value) => Encoding.UTF8.GetByteCount(value);
    }
}
