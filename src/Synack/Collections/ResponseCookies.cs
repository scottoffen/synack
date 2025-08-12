using System.Collections;
using Synack.Cookies;

namespace Synack.Collections;

/// <summary>
/// Mutable collection of response cookies. Becomes read-only after <see cref="Seal"/>.
/// </summary>
/// <remarks>
/// This type is not thread-safe; use a separate instance per response.
/// </remarks>
public sealed class ResponseCookies : IEnumerable<Cookie>
{
    internal static readonly string MessageCookiesAreReadOnly = "Response has already started; cookies are now read-only.";

    private readonly List<Cookie> _cookies = new(capacity: 4);
    private bool _sealed;

    // Snapshot of serialized Set-Cookie lines captured at seal time.
    private string[]? _frozen;

    /// <summary>Gets the number of cookies currently in the collection.</summary>
    public int Count => _cookies.Count;

    /// <summary>Gets whether the collection has been sealed (read-only).</summary>
    internal bool IsSealed => _sealed;

    /// <summary>
    /// Adds a cookie instance to the collection.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the collection is sealed.</exception>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="cookie"/> is <c>null</c>.</exception>
    public void Add(Cookie cookie)
    {
        ThrowIfSealed();
        ArgumentNullException.ThrowIfNull(cookie);
        _cookies.Add(cookie);
    }

    /// <summary>
    /// Creates and adds a cookie with the specified name and value.
    /// </summary>
    /// <returns>The added <see cref="Cookie"/> so callers can configure attributes before the response starts.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the collection is sealed.</exception>
    public Cookie Add(string name, string value)
    {
        ThrowIfSealed();
        var c = new Cookie(name, value);
        _cookies.Add(c);
        return c;
    }

    /// <summary>
    /// Adds a "deletion" cookie (empty value, expired) with the given scope.
    /// </summary>
    /// <param name="name">Cookie name to delete.</param>
    /// <param name="path">Path to match; defaults to <c>"/"</c>.</param>
    /// <param name="domain">Domain to match; <c>null</c> omits the attribute.</param>
    /// <exception cref="InvalidOperationException">Thrown if the collection is sealed.</exception>
    public void Delete(string name, string path = "/", string? domain = null)
    {
        ThrowIfSealed();
        _cookies.Add(Cookie.Delete(name, path, domain));
    }

    /// <summary>
    /// Removes the specified cookie instance from the collection.
    /// </summary>
    /// <returns><c>true</c> if the cookie was removed; otherwise, <c>false</c>.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the collection is sealed.</exception>
    public bool Remove(Cookie cookie)
    {
        ThrowIfSealed();
        ArgumentNullException.ThrowIfNull(cookie);
        return _cookies.Remove(cookie);
    }

    /// <summary>Clears all cookies from the collection.</summary>
    /// <exception cref="InvalidOperationException">Thrown if the collection is sealed.</exception>
    public void Clear()
    {
        ThrowIfSealed();
        _cookies.Clear();
        _frozen = null;
    }

    /// <summary>
    /// Marks the collection read-only and captures a snapshot of <c>Set-Cookie</c> header lines.
    /// Further mutations will throw.
    /// </summary>
    internal void Seal()
    {
        if (_sealed) return;
        _sealed = true;

        // Snapshot as serialized strings. Later writes use this snapshot,
        // so changes to Cookie objects post-seal do not affect the response.
        var n = _cookies.Count;
        if (n == 0)
        {
            _frozen = [];
            return;
        }

        var lines = new string[n];
        for (var i = 0; i < n; i++)
            lines[i] = _cookies[i].ToString();

        _frozen = lines;
    }

    /// <summary>
    /// Gets the sealed snapshot of <c>Set-Cookie</c> header lines.
    /// </summary>
    /// <remarks>
    /// Only valid after <see cref="Seal"/>; otherwise throws to catch misuse.
    /// </remarks>
    internal IReadOnlyList<string> GetSetCookieHeaderValues()
    {
        if (!_sealed)
            throw new InvalidOperationException("Response has not started; cookies are not sealed.");
        return _frozen ?? [];
    }

    /// <summary>Returns an enumerator over the cookies.</summary>
    /// <remarks>
    /// After sealing, enumeration still returns the original <see cref="Cookie"/> instances for inspection.
    /// Mutating them will not affect the response output captured at seal time.
    /// </remarks>
    public IEnumerator<Cookie> GetEnumerator() => _cookies.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private void ThrowIfSealed()
    {
        if (_sealed)
            throw new InvalidOperationException(MessageCookiesAreReadOnly);
    }
}
