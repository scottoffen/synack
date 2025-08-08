namespace Synack;

/// <summary>
/// Represents the incoming HTTP request, including request line values, headers, body stream,
/// and parsed data such as cookies and query parameters.
/// </summary>
public interface IHttpRequest
{
    /// <summary>
    /// Gets the request body as a stream. May be empty or unreadable depending on the request method or content headers.
    /// </summary>
    Stream Body { get; }

    /// <summary>
    /// Gets the collection of cookies parsed from the Cookie header.
    /// </summary>
    IReadOnlyDictionary<string, string> Cookies { get; }

    /// <summary>
    /// Gets the value of the Content-Length header, if present.
    /// </summary>
    long? ContentLength { get; }

    /// <summary>
    /// Gets the value of the Content-Type header, if present.
    /// </summary>
    string? ContentType { get; }

    /// <summary>
    /// Gets the collection of request headers, where each key maps to one or more values.
    /// </summary>
    IReadOnlyDictionary<string, string[]> Headers { get; }

    /// <summary>
    /// Indicates whether the request includes a body, based on Content-Length or Transfer-Encoding headers.
    /// </summary>
    bool HasBody { get; }

    /// <summary>
    /// Indicates whether the request was received over a secure connection (e.g. TLS/HTTPS).
    /// </summary>
    bool IsSecure { get; }

    /// <summary>
    /// Gets the HTTP method used for the request (e.g. "GET", "POST").
    /// </summary>
    string Method { get; }

    /// <summary>
    /// Gets the request path portion of the URL (excluding query string).
    /// </summary>
    string Path { get; }

    /// <summary>
    /// Gets the HTTP protocol version used by the client (e.g. "HTTP/1.1").
    /// </summary>
    string Protocol { get; }

    /// <summary>
    /// Gets the raw target from the request line, which may include encoded or unparsed path and query data.
    /// </summary>
    string RawTarget { get; }

    /// <summary>
    /// Gets the raw query string portion of the URL, including the leading '?' character if present.
    /// </summary>
    string QueryString { get; }

    /// <summary>
    /// Gets the collection of query string parameters, parsed into a mapping of keys to one or more values.
    /// </summary>
    IReadOnlyDictionary<string, string[]> Query { get; }

    /// <summary>
    /// Gets the fully reconstructed request URI, including scheme, host, path, and query.
    /// </summary>
    Uri Url { get; }
}


