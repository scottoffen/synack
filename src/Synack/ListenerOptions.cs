using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Synack.Authentication;
using Synack.Certificates;
using Synack.Diagnostics;
using Synack.Exceptions;

namespace Synack;

/// <summary>
/// Represents configuration options for an individual server listener.
/// </summary>
[ExcludeFromCodeCoverage]
public class ListenerOptions
{
    private X509Certificate2? _certificate;

    /// <summary>
    /// Gets or sets the authentication handler used to process incoming HTTP requests.
    /// </summary>
    /// <remarks>
    /// If <c>null</c>, no authentication will be performed and all requests will be assigned an anonymous principal.
    /// When set, the handler will be invoked once per request to determine the user identity associated with the request.
    /// </remarks>
    public IAuthenticationHandler? AuthenticationHandler { get; set; }

    /// <summary>
    /// Gets or sets the IP address the handler should bind to when listening for incoming connections.
    /// </summary>
    /// <remarks>
    /// This address determines which network interfaces the server will accept connections from:
    /// <list type="bullet">
    ///     <item>
    ///         <see cref="System.Net.IPAddress.Any"/> (default) binds to all available IPv4 interfaces (0.0.0.0).
    ///         This allows external clients on the network to connect.
    ///     </item>
    ///     <item>
    ///         <see cref="System.Net.IPAddress.Loopback"/> binds only to the local loopback interface (127.0.0.1),
    ///         preventing external connections — useful for development or internal services.
    ///     </item>
    ///     <item>
    ///         You may also bind to a specific interface IP (e.g., 192.168.1.10) to restrict connections to that address.
    ///     </item>
    ///     <item>
    ///         For IPv6, use <see cref="System.Net.IPAddress.IPv6Any"/> or <see cref="System.Net.IPAddress.IPv6Loopback"/>.
    ///     </item>
    /// </list>
    /// <para>If a hostname needs to be used (e.g., "localhost" or "example.com"), resolve it using DNS and assign the
    /// resulting IP address to this property before starting the handler.
    /// </para>
    /// <para>
    /// The default value is <see cref="System.Net.IPAddress.Any"/>.
    /// </para>
    /// </remarks>
    public IPAddress BindAddress { get; set; } = IPAddress.Any;

    /// <summary>
    /// Gets or sets the X.509 certificate used for TLS encryption.
    /// Setting this property sets <see cref="EnableTls"/> to true, and tells the listener to validate the certificate upon connection.
    /// </summary>
    /// <exception cref="InvalidCertificateException">Thrown if the certificate is invalid.</exception>
    public X509Certificate2? Certificate
    {
        get => _certificate;
        set
        {
            if (value == null)
            {
                _certificate = null;
                return;
            }

            if (!value.IsValid(out var issues))
            {
                throw new InvalidCertificateException(issues);
            }

            _certificate = value;
        }
    }

    /// <summary>
    /// If true, certificate revocation checks will be enabled during validation.
    /// </summary>
    /// <remarks>
    /// This is only applicable for mTLS (mutual TLS) scenarios.
    /// </remarks>
    public bool CheckCertificateRevocation { get; set; } = false;

    /// <summary>
    /// Optional delegate to validate a client certificate. If not set, the default platform validation is used.
    /// </summary>
    /// <remarks>
    /// This is only applicable for mTLS (mutual TLS) scenarios.
    /// </remarks>
    public Func<X509Certificate2, bool>? ClientCertificateValidator { get; set; }

    /// <summary>
    /// Gets a value indicating whether TLS is enabled for this listener.
    /// TLS is enabled when a valid <see cref="Certificate"/> is provided.
    /// </summary>
    public bool EnableTls => _certificate != null;

    /// <summary>
    /// Gets or sets the provider used to generate trace identifiers for incoming requests.
    /// </summary>
    /// <remarks>
    /// If no provider is specified, a default <see cref="GuidTraceIdentifierProvider"/> is used.
    ///
    /// Trace identifiers are used for logging, diagnostics, and request correlation. They are generated
    /// once per request or connection and exposed via the <c>TraceIdentifier</c> property on the context.
    ///
    /// <para>
    /// This property is assigned per listener. If multiple listeners are configured with different
    /// <see cref="ITraceIdentifierProvider"/> instances, the format and semantics of trace identifiers
    /// may vary across connections. To ensure consistent behavior, assign the same provider instance to
    /// all listeners unless intentional variation is desired.
    /// </para>
    /// </remarks>
    public ITraceIdentifierProvider TraceIdentifierProvider { get; set; } = new GuidTraceIdentifierProvider();

    /// <summary>
    /// Gets or sets the port number this listener binds to.
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// Gets or sets the list of URL prefixes this listener will handle.
    /// </summary>
    public List<string> Prefixes { get; set; } = [];

    /// <summary>
    /// If true, the listener will request and require a client certificate during the TLS handshake.
    /// </summary>
    /// <remarks>
    /// This enables (and requires) mTLS for the listener.
    /// </remarks>
    public bool RequireClientCertificate { get; set; } = false;
}
