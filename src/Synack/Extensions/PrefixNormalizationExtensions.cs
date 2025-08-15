using System.Buffers;

namespace Synack.Extensions;

/*
 * Public API: one extension method. Implementation is TFM-specific.
 */
internal static class PrefixNormalizationExtensions
{
    /// <summary>
    /// Normalizes a path-only routing prefix.
    /// Ensures leading '/', ensures trailing '/', collapses multiple slashes,
    /// converts '\' to '/', resolves '.' and '..', and rejects scheme/query/fragment.
    /// Drive-letter file paths (e.g., 'C:\foo') are not allowed.
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public static string NormalizePrefix(this string prefix)
    {
#if NET6_0_OR_GREATER
        return NormalizePrefix_Span(prefix);
#else
        return NormalizePrefix_String(prefix);
#endif
    }

#if NET6_0_OR_GREATER
    // -------------------------
    // .NET 6+ span-optimized
    // -------------------------
    private static string NormalizePrefix_Span(string prefix)
    {
        if (prefix is null) throw new ArgumentNullException(nameof(prefix));
        ReadOnlySpan<char> s = prefix.AsSpan().Trim();

        if (s.Length == 0) throw new ArgumentException("Prefix cannot be empty.", nameof(prefix));

        // Must be path-only
        if (s.IndexOf("://".AsSpan(), StringComparison.Ordinal) >= 0)
            throw new ArgumentException("Prefixes must be path-only (no scheme/host/port).", nameof(prefix));

        // Reject query/fragment
        if (s.IndexOfAny(stackalloc char[] { '?', '#' }) >= 0)
            throw new ArgumentException("Prefixes may not contain query or fragment.", nameof(prefix));

        // Reject Windows drive-letter paths like "C:\foo"
        if (s.Length >= 3 &&
            ((uint)(s[0] - 'A') <= ('Z' - 'A') || (uint)(s[0] - 'a') <= ('z' - 'a')) &&
            s[1] == ':' && (s[2] == '\\' || s[2] == '/'))
        {
            throw new ArgumentException("Prefixes must be URL paths, not drive-letter file paths.", nameof(prefix));
        }

        // Ensure leading '/'
        if (s[0] != '/')
        {
            var tmp = new char[s.Length + 1];
            tmp[0] = '/';
            s.CopyTo(tmp.AsSpan(1));
            s = tmp;
        }

        // Convert '\' to '/', collapse slashes, resolve dot segments in one pass
        var pool = ArrayPool<char>.Shared;
        char[]? rented = null;
        Span<char> dst = s.Length <= 256 ? stackalloc char[256] : (rented = pool.Rent(s.Length + 2));
        int d = 0;

        // Segment stack (positions of segment starts in dst)
        int segCap = Math.Min(128, Math.Max(1, s.Length)); // avoid zero-length stackalloc
        Span<int> segStack = stackalloc int[segCap];
        List<int>? segList = null;
        int segCount = 0;

        // Helpers (static to avoid capturing ref structs)
        static void PushSeg(Span<int> stack, ref int count, ref List<int>? list, int idx)
        {
            if (list is null)
            {
                if (count < stack.Length)
                {
                    stack[count++] = idx;
                }
                else
                {
                    list = new List<int>(Math.Max(count * 2, 32));
                    for (int i = 0; i < count; i++) list.Add(stack[i]);
                    list.Add(idx);
                }
            }
            else
            {
                list.Add(idx);
            }
        }

        static int PopSeg(Span<int> stack, ref int count, ref List<int>? list)
        {
            if (list is null)
            {
                if (count == 0) return -1;
                return stack[--count];
            }
            if (list.Count == 0) return -1;
            int idx = list[^1];
            list.RemoveAt(list.Count - 1);
            return idx;
        }

        bool inSlash = false;
        int segStart = -1;

        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i] == '\\' ? '/' : s[i];

            if (c == '/')
            {
                if (!inSlash)
                {
                    // Close previous segment
                    if (segStart >= 0)
                    {
                        int segLen = d - segStart;
                        if (segLen == 1 && dst[segStart] == '.')
                        {
                            // drop "." and treat as if we're already at a slash
                            d = segStart;
                            inSlash = true;
                            segStart = -1;
                            continue;
                        }
                        else if (segLen == 2 && dst[segStart] == '.' && dst[segStart + 1] == '.')
                        {
                            // drop ".." and previous segment; never remove root
                            d = segStart;
                            int prevStart = PopSeg(segStack, ref segCount, ref segList);
                            if (prevStart >= 0)
                            {
                                int slashPos = prevStart - 1; // slash before previous segment
                                if (slashPos <= 0)
                                {
                                    dst[0] = '/';
                                    d = 1;
                                }
                                else
                                {
                                    // keep the slash and position AFTER it so next segment doesn't overwrite it
                                    dst[slashPos] = '/';
                                    d = slashPos + 1;
                                }
                            }
                            else
                            {
                                dst[0] = '/';
                                d = 1;
                            }
                            inSlash = true;   // conceptually at a slash boundary
                            segStart = -1;
                            continue;         // skip writing another slash
                        }
                        else
                        {
                            PushSeg(segStack, ref segCount, ref segList, segStart);
                        }
                        segStart = -1;
                    }

                    // write single slash
                    dst[d++] = '/';
                    inSlash = true;
                }
                // else: collapse extra slashes
            }
            else
            {
                inSlash = false;
                if (segStart < 0) segStart = d;
                dst[d++] = c;
            }
        }

        // Finalize last segment if any
        if (segStart >= 0)
        {
            int segLen = d - segStart;
            if (segLen == 1 && dst[segStart] == '.')
            {
                d = segStart; // drop "."
            }
            else if (segLen == 2 && dst[segStart] == '.' && dst[segStart + 1] == '.')
            {
                d = segStart; // drop ".."
                int prevStart = PopSeg(segStack, ref segCount, ref segList);
                if (prevStart >= 0)
                {
                    int slashPos = prevStart - 1;
                    if (slashPos <= 0)
                    {
                        dst[0] = '/';
                        d = 1;
                    }
                    else
                    {
                        dst[slashPos] = '/';
                        d = slashPos + 1;
                    }
                }
                else
                {
                    dst[0] = '/';
                    d = 1;
                }
            }
            else
            {
                PushSeg(segStack, ref segCount, ref segList, segStart);
            }
        }

        // Ensure trailing '/'
        if (d == 0 || dst[d - 1] != '/')
        {
            dst[d++] = '/';
        }

        var result = new string(dst.Slice(0, d));
        if (rented is not null) pool.Return(rented);
        return result;
    }
#else
    // -------------------------
    // .NET Standard 2.0 fallback
    // -------------------------
    private static string NormalizePrefix_String(string prefix)
    {
        if (prefix == null) throw new ArgumentNullException(nameof(prefix));

        var s = prefix.Trim();
        if (s.Length == 0) throw new ArgumentException("Prefix cannot be empty.", nameof(prefix));

        if (s.IndexOf("://", StringComparison.Ordinal) >= 0)
            throw new ArgumentException("Prefixes must be path-only (no scheme/host/port).", nameof(prefix));

        if (s.IndexOfAny(new[] { '?', '#' }) >= 0)
            throw new ArgumentException("Prefixes may not contain query or fragment.", nameof(prefix));

        // Drive-letter guard (e.g., "C:\foo")
        if (s.Length >= 3 &&
            char.IsLetter(s[0]) && s[1] == ':' && (s[2] == '\\' || s[2] == '/'))
        {
            throw new ArgumentException("Prefixes must be URL paths, not drive-letter file paths.", nameof(prefix));
        }

        s = s.Replace('\\', '/');

        if (s[0] != '/')
            s = "/" + s;

        // Collapse multiple slashes
        if (s.IndexOf("//", StringComparison.Ordinal) >= 0)
        {
            var sb = new StringBuilder(s.Length);
            char last = '\0';
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '/' && last == '/') continue;
                sb.Append(c);
                last = c;
            }
            s = sb.ToString();
        }

        // Remove dot segments
        var parts = s.Split(new[] { '/' }, StringSplitOptions.None);
        var stack = new List<string>(parts.Length);
        foreach (var seg in parts)
        {
            if (string.IsNullOrEmpty(seg) || seg == ".") continue;
            if (seg == "..")
            {
                if (stack.Count > 0) stack.RemoveAt(stack.Count - 1);
                continue;
            }
            stack.Add(seg);
        }

        if (stack.Count == 0)
        {
            s = "/";
        }
        else
        {
            var sb = new StringBuilder(s.Length);
            for (int i = 0; i < stack.Count; i++)
            {
                sb.Append('/').Append(stack[i]);
            }
            s = sb.ToString();
        }

        if (!s.EndsWith("/", StringComparison.Ordinal)) s += "/";

        return s;
    }
#endif
}
