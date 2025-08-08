using System.Security.Claims;

namespace Synack.Authentication;

/// <summary>
/// Defines a contract for authenticating an incoming HTTP request.
/// </summary>
/// <remarks>
/// <para>
/// Implementations are responsible for inspecting the request context and returning a <see cref="ClaimsPrincipal"/>
/// that represents the authenticated user. If authentication fails or is not applicable, return a principal with
/// an unauthenticated identity. Do not throw exceptions for unauthenticated requests; this interface does not 
/// distinguish between anonymous and failed authentication.
/// </para>
/// <para>
/// Returning <c>null</c> is not supported and will result in the connection being closed.
/// </para>
/// <para>
/// Implementations should be thread-safe and non-blocking wherever possible.
/// </para>
/// </remarks>
public interface IAuthenticationHandler
{
    /// <summary>
    /// Authenticates the specified HTTP request and returns a corresponding <see cref="ClaimsPrincipal"/>.
    /// </summary>
    /// <remarks>
    /// <para
    /// >The returned principal should represent either an authenticated user or an anonymous identity. Do not return <c>null</c>.
    /// Throwing exceptions should be reserved for unexpected internal errors, not for authentication failures.
    /// </para>
    /// <para>
    /// The <paramref name="context"/> may be used to inspect headers, client certificates, or other request data.
    /// </para>
    /// </remarks>
    Task<ClaimsPrincipal> AuthenticateAsync(IHttpContext context, CancellationToken cancellationToken = default);
}

