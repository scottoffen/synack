using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Synack.Cookies;

/// <summary>
/// Represents a single HTTP cookie to be sent with a <c>Set-Cookie</c> header.
/// </summary>
[DebuggerDisplay("{Name}={Value}")]
public sealed class Cookie
{
    private static readonly char[] InvalidDomainChars = [' ', '\t', ';', '/', '\\'];

    internal static readonly string MessageCookieNameMissing = "Cookie name cannot be null, empty, or whitespace.";
    internal static readonly string MessageCookieNameInvalid = "Invalid characters in cookie name.";
    internal static readonly string MessageCookieValueInvalid = "Cookie value cannot contain control characters, CR/LF, commas, or ';'.";
    internal static readonly string MessageCookieDomainMissing = "Domain cannot be empty.";
    internal static readonly string MessageCookieDomainFormatInvalid = "Invalid domain format";
    internal static readonly string MessageCookieDomainInvalid = "Invalid domain (IDN conversion failed).";
    internal static readonly string MessageCookiePathMissing = "Path cannot be null or empty.";
    internal static readonly string MessageCookiePathMalformed = "Path must start with '/'.";
    internal static readonly string MessageCookiePathInvalid = "Path cannot contain control characters, CR/LF, or ';'.";
    internal static readonly string MessageCookieExpiresInvalid = "Expires must be in UTC.";
    internal static readonly string MessageCookieMaxAgeMustBeNonNegative = "Max-Age must be >= 0.";
    internal static readonly string MessageSameSiteMustBeSecure = "Cookies with SameSite=None must be Secure.";
    internal static readonly string MessagePartitionedMustBeSecure = "Partitioned cookies must be Secure.";

    private string _name = string.Empty;
    private string _value = string.Empty;
    private string? _domain; // null -> omit attribute
    private string _path = "/";
    private DateTime? _expires;
    private int? _maxAge;

    /// <summary>
    /// Gets or sets the cookie name (RFC 6265 token).
    /// </summary>
    /// <remarks>
    /// Must be non-empty and contain only valid token characters; otherwise an <see cref="ArgumentException"/> is thrown.
    /// </remarks>
    public string Name
    {
        get => _name;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException(MessageCookieNameMissing, nameof(value));
            if (!IsValidToken(value))
                throw new ArgumentException(MessageCookieNameInvalid, nameof(value));
            _name = value;
        }
    }

    /// <summary>
    /// Gets or sets the cookie value.
    /// </summary>
    /// <remarks>
    /// Must not be <c>null</c>, contain control characters, CR/LF, or the <c>';'</c> or <c>,</c> characters; otherwise an <see cref="ArgumentException"/> is thrown.
    /// </remarks>
    public string Value
    {
        get => _value;
        set
        {
            if (value is null) throw new ArgumentNullException(nameof(value));
            if (ContainsCtlOrCrLf(value) || value.IndexOfAny([';', ',']) >= 0)
                throw new ArgumentException(MessageCookieValueInvalid, nameof(value));
            _value = value;
        }
    }

    /// <summary>
    /// Gets or sets the cookie <c>Domain</c> attribute, or <c>null</c> to omit it.
    /// </summary>
    /// <remarks>
    /// When set, the value is normalized (leading dot removed, lower-cased, converted to ASCII via IDN).
    /// Invalid formats throw an <see cref="ArgumentException"/>.
    /// </remarks>
    public string? Domain
    {
        get => _domain;
        set
        {
            if (value is null) { _domain = null; return; }
            var v = value.Trim().TrimStart('.').ToLowerInvariant();
            if (v.Length == 0)
                throw new ArgumentException(MessageCookieDomainMissing, nameof(value));
            if (v.IndexOfAny(InvalidDomainChars) >= 0 || v.Contains(':'))
                throw new ArgumentException(MessageCookieDomainFormatInvalid, nameof(value));
            // Optional IDN mapping to ASCII
            try
            {
                var idn = new IdnMapping();
                v = idn.GetAscii(v);
            }
            catch (ArgumentException)
            {
                throw new ArgumentException(MessageCookieDomainInvalid, nameof(value));
            }
            _domain = v;
        }
    }

    /// <summary>
    /// Gets or sets the cookie <c>Path</c> attribute.
    /// </summary>
    /// <remarks>
    /// Must start with <c>'/'</c> and contain no control characters, CR/LF, or <c>';'</c>. The default is <c>"/"</c>.
    /// </remarks>
    public string Path
    {
        get => _path;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException(MessageCookiePathMissing, nameof(value));
            if (!value.StartsWith("/", StringComparison.Ordinal))
                throw new ArgumentException(MessageCookiePathMalformed, nameof(value));
            if (ContainsCtlOrCrLf(value) || value.IndexOf(';') >= 0)
                throw new ArgumentException(MessageCookiePathInvalid, nameof(value));
            _path = value;
        }
    }

    /// <summary>
    /// Gets or sets the cookie <c>Expires</c> attribute in UTC, or <c>null</c> to omit it.
    /// </summary>
    /// <remarks>
    /// A non-null value must have <see cref="DateTime.Kind"/> of <see cref="DateTimeKind.Utc"/>; otherwise an exception is thrown.
    /// </remarks>
    public DateTime? Expires
    {
        get => _expires;
        set
        {
            if (value is null) { _expires = null; return; }
            if (value.Value.Kind != DateTimeKind.Utc)
                throw new ArgumentException(MessageCookieExpiresInvalid, nameof(value));
            _expires = value;
        }
    }

    /// <summary>
    /// Gets or sets the cookie <c>Max-Age</c> attribute, in seconds, or <c>null</c> to omit it.
    /// </summary>
    /// <remarks>
    /// Must be greater than or equal to zero when set; otherwise an <see cref="ArgumentOutOfRangeException"/> is thrown.
    /// </remarks>
    public int? MaxAge
    {
        get => _maxAge;
        set
        {
            if (value is { } v && v < 0)
                throw new ArgumentOutOfRangeException(nameof(value), MessageCookieMaxAgeMustBeNonNegative);
            _maxAge = value;
        }
    }

    /// <summary>
    /// Gets or sets whether to include the <c>Secure</c> attribute.
    /// </summary>
    public bool Secure { get; set; }

    /// <summary>
    /// Gets or sets whether to include the <c>HttpOnly</c> attribute.
    /// </summary>
    public bool HttpOnly { get; set; }

    /// <summary>
    /// Gets or sets the cookie <c>SameSite</c> attribute.
    /// </summary>
    /// <remarks>
    /// When set to <see cref="SameSiteMode.None"/>, <see cref="Secure"/> must be <c>true</c> or serialization will throw an <see cref="InvalidOperationException"/>.
    /// </remarks>
    public SameSiteMode? SameSite { get; set; } // None, Lax, Strict

    /// <summary>
    /// Gets or sets the cookie <c>Priority</c> attribute (Chromium).
    /// </summary>
    /// <remarks>
    /// If <c>null</c>, the attribute is omitted.
    /// </remarks>
    public CookiePriority? Priority { get; set; } // Low/Medium/High

    /// <summary>
    /// Gets or sets whether to include the <c>Partitioned</c> attribute (CHIPS).
    /// </summary>
    /// <remarks>
    /// Modern user agents generally require <see cref="Secure"/> when <c>Partitioned</c> is set.
    /// </remarks>
    public bool Partitioned { get; set; }

    public Cookie(string name, string value)
    {
        Name = name;
        Value = value;
    }

    /// <summary>
    /// Sets <see cref="MaxAge"/> and <see cref="Expires"/> to expire the cookie after the specified time-to-live.
    /// </summary>
    /// <param name="ttl">The time-to-live duration. Values &lt;= 0 expire immediately.</param>
    /// <remarks>
    /// This helper sets <see cref="MaxAge"/> to the floor of <paramref name="ttl"/> in whole seconds (minimum 0) and <see cref="Expires"/> to <c>DateTime.UtcNow + ttl</c>.
    /// </remarks>
    public void ExpireIn(TimeSpan ttl)
    {
        if (ttl <= TimeSpan.Zero)
        {
            MaxAge = 0;
            Expires = DateTime.UtcNow;
            return;
        }

        // Floor to whole seconds so Max-Age and Expires agree
        var seconds = ttl.TotalSeconds >= int.MaxValue
            ? int.MaxValue
            : (int)Math.Floor(ttl.TotalSeconds);

        MaxAge = seconds;
        Expires = DateTime.UtcNow.AddSeconds(seconds);
    }

    /// <summary>
    /// Returns the <c>Set-Cookie</c> header string for this cookie.
    /// </summary>
    /// <remarks>
    /// Throws <see cref="InvalidOperationException"/> if <see cref="SameSite"/> is <see cref="SameSiteMode.None"/> and <see cref="Secure"/> is <c>false</c>.
    /// </remarks>
    public override string ToString()
    {
        // Validate cross-attribute rules just before serialization.
        if (SameSite == SameSiteMode.None && !Secure)
            throw new InvalidOperationException(MessageSameSiteMustBeSecure);

        if (Partitioned && !Secure)
            throw new InvalidOperationException(MessagePartitionedMustBeSecure);

        var sb = new StringBuilder(Name.Length + Value.Length + 96);
        sb.Append(Name).Append('=').Append(Value);

        if (_domain is not null)
            sb.Append("; Domain=").Append(_domain);
        if (!string.IsNullOrEmpty(_path))
            sb.Append("; Path=").Append(_path);
        if (MaxAge.HasValue)
            sb.Append("; Max-Age=").Append(MaxAge.Value);
        if (Expires.HasValue)
            sb.Append("; Expires=").Append(Expires.Value.ToString("R", CultureInfo.InvariantCulture));
        if (Secure)
            sb.Append("; Secure");
        if (HttpOnly)
            sb.Append("; HttpOnly");
        if (SameSite.HasValue)
            sb.Append("; SameSite=").Append(SameSiteToToken(SameSite.Value));
        if (Priority.HasValue)
            sb.Append("; Priority=").Append(PriorityToToken(Priority.Value));
        if (Partitioned)
            sb.Append("; Partitioned");

        return sb.ToString();
    }

    /// <summary>
    /// Creates a cookie that instructs the client to delete it.
    /// </summary>
    /// <param name="name">The cookie name to delete.</param>
    /// <param name="path">The cookie path to match; defaults to <c>"/"</c>.</param>
    /// <param name="domain">The cookie domain to match; <c>null</c> omits the attribute.</param>
    /// <returns>A cookie with empty value, <c>Max-Age=0</c>, and an <c>Expires</c> date in the past.</returns>
    /// <remarks>
    /// <para>For deletion to succeed, <paramref name="path"/> and <paramref name="domain"/> should match the original cookie's attributes.</para>
    /// If the original cookie had SameSite=None and Secure, or was Partitioned,
    /// the caller should set those on the returned cookie to match.
    /// </remarks>
    public static Cookie Delete(string name, string path = "/", string? domain = null)
    {
        // Constructor + setters enforce token/name/value/path/domain invariants
        var cookie = new Cookie(name, string.Empty)
        {
            // Expire immediately
            MaxAge = 0,
            Expires = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc),

            // Scope must match the original cookie to ensure deletion
            Path = path ?? "/"
        };

        if (domain is not null)
            cookie.Domain = domain;

        // NOTE:
        // If the original cookie had SameSite=None and Secure, or was Partitioned,
        // the caller should set those on the returned cookie to match:
        // cookie.Secure = true; cookie.SameSite = SameSiteMode.None; cookie.Partitioned = true;

        return cookie;
    }

    private static bool IsValidToken(string input)
    {
        foreach (var c in input)
        {
            if (c <= 0x20 || c >= 0x7f || "()<>@,;:\\\"/[]?={} \t".IndexOf(c) >= 0)
                return false;
        }
        return true;
    }

    private static bool ContainsCtlOrCrLf(string s)
    {
        for (var i = 0; i < s.Length; i++)
        {
            var c = s[i];
            if (c == '\r' || c == '\n') return true;
            if (c <= 0x1F || c == 0x7F) return true;
        }
        return false;
    }

    private static string SameSiteToToken(SameSiteMode mode) => mode switch
    {
        SameSiteMode.Lax => "Lax",
        SameSiteMode.Strict => "Strict",
        _ => "None"
    };

    private static string PriorityToToken(CookiePriority p) => p switch
    {
        CookiePriority.Low => "Low",
        CookiePriority.Medium => "Medium",
        _ => "High"
    };
}
