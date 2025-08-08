namespace Synack;

/// <summary>
/// Represents the HTTP response to be sent to the client, including status code, headers, body content,
/// and lifecycle control methods.
/// </summary>
public interface IHttpResponse
{
    /// <summary>
    /// Gets the stream used to write the response body back to the client.
    /// </summary>
    Stream Body { get; }

    /// <summary>
    /// Gets the collection of response headers, where each key maps to one or more values.
    /// </summary>
    IDictionary<string, string[]> Headers { get; }

    /// <summary>
    /// Indicates whether any part of the response has been sent to the client.
    /// </summary>
    bool HasStarted { get; }

    /// <summary>
    /// Gets or sets the optional reason phrase sent with the HTTP status line (e.g. "Not Found").
    /// This value is only used in HTTP/1.x responses and is ignored for HTTP/2 and newer protocols.
    /// </summary>
    string? ReasonPhrase { get; set; }

    /// <summary>
    /// Disables response buffering and transitions to streaming mode, forcing headers to be sent immediately.
    /// </summary>
    void StartStreaming();

    /// <summary>
    /// Gets or sets the HTTP status code to return to the client (e.g. 200, 404).
    /// </summary>
    int StatusCode { get; set; }

    /// <summary>
    /// Aborts the response immediately, terminating the connection without completing the response.
    /// </summary>
    void Abort();

    /// <summary>
    /// Sets the response status code to 302 and adds a Location header with the specified URL.
    /// </summary>
    /// <param name="url">The URL to redirect the client to.</param>
    void Redirect(string url);
}
