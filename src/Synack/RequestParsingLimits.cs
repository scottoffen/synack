using System.Diagnostics.CodeAnalysis;

namespace Synack;

/// <summary>
/// Configurable limits for parsing HTTP request components.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class RequestParsingLimits
{
    /// <summary>
    /// Gets or sets the maximum size, in bytes, allowed for a single cookie in the request.
    /// </summary>
    /// <remarks>
    /// The default value is <c>null</c>, which means there is no limit.
    /// </remarks>
    public int? MaxCookieBytesPerName { get; set; }

    /// <summary>
    /// Gets or sets the maximum combined size, in bytes, of all cookies in the request.
    /// </summary>
    /// <remarks>
    /// The default value is <c>null</c>, which means there is no limit.
    /// </remarks>
    public int? MaxCookiesBytesTotal { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of cookies allowed in the request.
    /// </summary>
    /// <remarks>
    /// The default value is <c>null</c>, which means there is no limit.
    /// </remarks>
    public int? MaxCookieCount { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of distinct request headers allowed.
    /// </summary>
    /// <remarks>
    /// The default value is <c>null</c>, which means there is no limit.
    /// </remarks>
    public int? MaxHeaderCount { get; set; }

    /// <summary>
    /// Gets or sets the maximum length, in bytes, of a single header field line
    /// (including the name, colon, and value).
    /// </summary>
    /// <remarks>
    /// The default value is <c>null</c>, which means there is no limit.
    /// </remarks>
    public int? MaxHeaderFieldLength { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of values allowed for a single header name.
    /// </summary>
    /// <remarks>
    /// The default value is <c>null</c>, which means there is no limit.
    /// </remarks>
    public int? MaxHeaderValuesPerName { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of distinct query string parameters allowed.
    /// </summary>
    /// <remarks>
    /// The default value is <c>null</c>, which means there is no limit.
    /// </remarks>
    public int? MaxQueryParameterCount { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of values allowed for a single query string key.
    /// </summary>
    /// <remarks>
    /// The default value is <c>null</c>, which means there is no limit.
    /// </remarks>
    public int? MaxQueryValuesPerKey { get; set; }
}
