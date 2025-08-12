using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Collections.ObjectModel;
#if NET8_0_OR_GREATER
using System.Collections.Frozen;
#endif

namespace Synack.Collections;

public sealed class ResponseHeaders : IEnumerable<KeyValuePair<string, IReadOnlyList<string>>>
{
    private static readonly StringComparer _comparer = StringComparer.OrdinalIgnoreCase;
    private static readonly string _setCookieHeader = "Set-Cookie";

    internal static readonly HashSet<string> SingletonHeaders = new(_comparer)
    {
        "Content-Length",
        "Content-Type",
        "Date",
        "Location",
        "ETag",
        "Last-Modified",
        "Expires"
    };

    internal static readonly string MessageCookieHeaderNotAllowed = "Use Response.Cookies to add cookies. Do not set the Set-Cookie header directly.";
    internal static readonly string MessageHeaderDoesNotAllowMultipleValues = "Header '{0}' does not support multiple values. Use Set() instead.";
    internal static readonly string MessageHeadersAreReadOnly = "Response has already started; headers are now read-only.";
    internal static readonly string MessageHeaderNameMissing = "Header name cannot be null or empty.";
    internal static readonly string MessageHeaderContainsCrLf = "Header {0} cannot contain CR or LF.";
    internal static readonly string MessageHeaderContainsCtrlChars = "Header {0} cannot contain control characters.";
    internal static readonly string MessageHeaderInvalidChar = "Header {0} contains invalid character {1}";

    internal static readonly string MessagePartHeaderName = "name";
    internal static readonly string MessagePartHeaderValue = "value";

    private readonly Dictionary<string, List<string>> _headers = new(_comparer);
    private bool _sealed;

    private IReadOnlyDictionary<string, string[]>? _frozen;

    public int Count => _headers.Count;

    internal bool IsSealed => _sealed;


    public void Append(string name, string value)
    {
        ThrowIfSealed();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ThrowIfSetCookie(name);

        ValidateHeaderName(name);
        ValidateHeaderValue(value);

        if (SingletonHeaders.Contains(name))
            throw new InvalidOperationException(string.Format(MessageHeaderDoesNotAllowMultipleValues, name));

        if (!_headers.TryGetValue(name, out var list))
        {
            list = new List<string>(2);
            _headers[name] = list;
        }

        list.Add(value);
    }

    public void Clear()
    {
        ThrowIfSealed();
        _headers.Clear();
        _frozen = null;
    }

    public bool ContainsKey(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return _sealed && _frozen is not null
            ? _frozen.ContainsKey(name)
            : _headers.ContainsKey(name);
    }

    public bool Remove(string name)
    {
        ThrowIfSealed();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return _headers.Remove(name);
    }

    internal void Seal()
    {
        if (_sealed) return;
        _sealed = true;

        var snapshot = new Dictionary<string, string[]>(_headers.Count, _comparer);
        foreach (var kvp in _headers)
        {
            snapshot[kvp.Key] = kvp.Value.ToArray();
        }

#if NET8_0_OR_GREATER
        _frozen = snapshot.ToFrozenDictionary(_comparer);
#else
        _frozen = snapshot;
#endif
    }

    public void Set(string name, string value)
    {
        ThrowIfSealed();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ThrowIfSetCookie(name);

        ValidateHeaderName(name);
        ValidateHeaderValue(value);

        if (!_headers.TryGetValue(name, out var list))
        {
            list = new List<string>(1);
            _headers[name] = list;
        }
        else
        {
            list.Clear();
        }

        list.Add(value);
    }

    public bool TryGetValues(string name, out IReadOnlyList<string> values)
    {
        if (!_sealed)
        {
            if (_headers.TryGetValue(name, out var list))
            {
                // Return a copy to avoid leaking the mutable list pre-seal.
                values = (list.Count == 0) ? Array.Empty<string>() : list.ToArray();
                return true;
            }

            values = [];
            return false;
        }

        // Post-seal: read from frozen snapshot and wrap to prevent casting back to string[] and mutating.
        if (_frozen is not null && _frozen.TryGetValue(name, out var arr))
        {
            values = (arr.Length == 0) ? Array.Empty<string>() : new ReadOnlyCollection<string>(arr);
            return true;
        }

        values = [];
        return false;
    }

    public IEnumerator<KeyValuePair<string, IReadOnlyList<string>>> GetEnumerator()
    {
        if (!_sealed)
        {
            // Pre-seal: yield copies/wrappers so callers can’t keep a live List reference.
            foreach (var kv in _headers)
            {
                var list = kv.Value;
                IReadOnlyList<string> ro = list.Count == 0
                    ? []
                    : list.ToArray();
                yield return new KeyValuePair<string, IReadOnlyList<string>>(kv.Key, ro);
            }
            yield break;
        }

        // Post-seal: enumerate the frozen snapshot.
        if (_frozen is not null)
        {
            foreach (var kv in _frozen)
            {
                var arr = kv.Value;
                IReadOnlyList<string> ro = arr.Length == 0
                    ? Array.Empty<string>()
                    : new ReadOnlyCollection<string>(arr);
                yield return new KeyValuePair<string, IReadOnlyList<string>>(kv.Key, ro);
            }
        }
    }

    [ExcludeFromCodeCoverage]
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private void ThrowIfSealed()
    {
        if (_sealed)
            throw new InvalidOperationException(MessageHeadersAreReadOnly);
    }

    private void ThrowIfSetCookie(string name)
    {
        if (_comparer.Equals(name, _setCookieHeader))
            throw new InvalidOperationException(MessageCookieHeaderNotAllowed);
    }

    private static void ValidateHeaderName(string name)
    {
        if (name.Length == 0)
            throw new ArgumentException(MessageHeaderNameMissing, nameof(name));

        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (c <= 31 || c == 127)
                throw new ArgumentException(string.Format(MessageHeaderContainsCtrlChars, MessagePartHeaderName), nameof(name));

            switch (c)
            {
                case '(':
                case ')':
                case '<':
                case '>':
                case '@':
                case ',':
                case ';':
                case ':':
                case '\\':
                case '\"':
                case '/':
                case '[':
                case ']':
                case '?':
                case '=':
                case '{':
                case '}':
                case ' ':
                case '\t':
                    throw new ArgumentException(
                        string.Format(MessageHeaderInvalidChar, MessagePartHeaderName, c),
                        nameof(name));
            }
        }
    }

    private static void ValidateHeaderValue(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        // Disallow CR/LF and control chars except HTAB (09) which many stacks allow.
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (c == '\r' || c == '\n')
                throw new ArgumentException(string.Format(MessageHeaderContainsCrLf, MessagePartHeaderValue), nameof(value));
            if ((c <= 31 && c != '\t') || c == 127)
                throw new ArgumentException(string.Format(MessageHeaderContainsCtrlChars, MessagePartHeaderValue), nameof(value));
        }
    }
}
