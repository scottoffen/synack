using System.Net;
using System.Security.Claims;

namespace Synack;

/// <summary>
/// Represents the context for an individual HTTP request and response pair,
/// including request/response data, connection metadata, user identity, and lifecycle hooks.
/// </summary>
public interface IHttpContext
{
    /// <summary>
    /// Gets the local network endpoint representing the server interface that accepted the connection.
    /// </summary>
    EndPoint LocalEndPoint { get; }

    /// <summary>
    /// Gets the remote network endpoint representing the client that initiated the connection.
    /// </summary>
    EndPoint RemoteEndPoint { get; }

    /// <summary>
    /// Gets the HTTP request associated with the current context.
    /// </summary>
    IHttpRequest Request { get; }

    /// <summary>
    /// Gets a cancellation token that is triggered if the client disconnects or the request is aborted.
    /// </summary>
    CancellationToken RequestAborted { get; }

    /// <summary>
    /// Gets the HTTP response associated with the current context.
    /// </summary>
    IHttpResponse Response { get; }

    /// <summary>
    /// Gets a unique identifier for the request, useful for tracing, logging, and correlation.
    /// </summary>
    string TraceIdentifier { get; }

    /// <summary>
    /// Gets or sets the security principal associated with the current request, if any.
    /// This value may be populated by authentication middleware or user-defined logic.
    /// </summary>
    ClaimsPrincipal User { get; set; }

    /// <summary>
    /// Registers a callback to be invoked after the response has been fully sent and the request lifecycle is complete.
    /// Callbacks are invoked in reverse order of registration and should not throw.
    /// </summary>
    /// <param name="callback">A delegate to invoke once the request has completed.</param>
    void OnCompleted(Func<Task> callback);
}
