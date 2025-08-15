using System.Collections.Immutable;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Synack.Authentication;
using Synack.Certificates;
using Synack.Diagnostics;
using Synack.Exceptions;
using Synack.Extensions;

namespace Synack;

/// <summary>
/// Represents configuration options for an individual server listener.
/// </summary>
public class ListenerOptions
{
    internal static readonly string MessageOptionsAreSealed = $"{nameof(ListenerOptions)} cannot be modified after being sealed.";
    internal static readonly string MessageBindAddressCannotBeNull = "BindAddress cannot be null.";
    internal static readonly string MessagePortOutOfRange = "Listener port must be between 0 and 65535.";

    private volatile IAuthenticationHandler? _authenticationHandler;
    private volatile Func<X509Certificate2, bool>? _certificateValidator;
    private volatile X509Certificate2? _certificate;
    private volatile bool _isSealed;
    private volatile ITraceIdentifierProvider _traceIdentifierProvider = new GuidTraceIdentifierProvider();

    private IPAddress _bindAddress = IPAddress.Any;
    private bool _checkCertificateRevocation = false;
    private int _port = 0;
    private ImmutableHashSet<string> _prefixes = ImmutableHashSet<string>.Empty.WithComparer(StringComparer.OrdinalIgnoreCase);
    private bool _requireClientCertificate = false;

    /// <summary>
    /// Adds a new URL prefix to the listener's prefix collection.
    /// </summary>
    /// <param name="prefix">The prefix to add. The value is normalized before being stored.</param>
    /// <remarks>
    /// This method can be called while the listener is running.  
    /// The new prefix is available for matching on subsequent requests.  
    /// Adding a duplicate (case-insensitive) prefix has no effect.  
    /// Prefix normalization is performed using the same rules and case-insensitive comparer  
    /// (<see cref="StringComparer.OrdinalIgnoreCase"/>) as the routing system.
    /// </remarks>
    public void AddPrefix(string prefix)
    {
        ImmutableInterlocked.Update(ref _prefixes, p => p.Add(prefix.NormalizePrefix()));
    }

    /// <summary>
    /// Gets or sets the authentication handler used to process incoming HTTP requests.
    /// </summary>
    /// <remarks>
    /// If <c>null</c>, no authentication will be performed and all requests will be assigned an anonymous principal.
    /// When set, the handler will be invoked once per request to determine the user identity associated with the request.
    /// </remarks>
    public IAuthenticationHandler? AuthenticationHandler
    {
        get => _authenticationHandler;
        set => _authenticationHandler = value;
    }

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
    public IPAddress BindAddress
    {
        get => _bindAddress;
        set
        {
            ThrowIfSealed();
            _bindAddress = value ?? throw new ArgumentNullException(nameof(value), MessageBindAddressCannotBeNull);
        }
    }

    /// <summary>
    /// Gets or sets the X.509 certificate used for TLS encryption.
    /// Setting this property tells the listener to validate the certificate upon connection.
    /// </summary>
    /// <remarks>
    /// TLS is considered enabled when a valid certificate is configured (affects new handshakes).
    /// </remarks>
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
    public bool CheckCertificateRevocation
    {
        get => _checkCertificateRevocation;
        set
        {
            ThrowIfSealed();
            _checkCertificateRevocation = value;
        }
    }

    /// <summary>
    /// Optional delegate to validate a client certificate. If not set, the default platform validation is used.
    /// </summary>
    /// <remarks>
    /// This is only applicable for mTLS (mutual TLS) scenarios.
    /// </remarks>
    public Func<X509Certificate2, bool>? ClientCertificateValidator
    {
        get => _certificateValidator;
        set => _certificateValidator = value;
    }

    /// <summary>
    /// Gets or sets the port number this listener binds to.
    /// </summary>
    public int Port
    {
        get => _port;
        set
        {
            ThrowIfSealed();
            if ((uint)value > 65535) throw new ArgumentOutOfRangeException(nameof(value), MessagePortOutOfRange);
            _port = value;
        }
    }

    /// <summary>
    /// Gets or sets the collection of URL prefixes this listener will handle.
    /// </summary>
    /// <remarks>
    /// Setting this property atomically replaces the entire prefix set with the specified collection.  
    /// Each prefix is normalized and stored using a case-insensitive comparer (<see cref="StringComparer.OrdinalIgnoreCase"/>).  
    /// Duplicate prefixes are automatically removed.  
    /// This property can be set while the listener is running; changes take effect for new requests.  
    /// Use <see cref="AddPrefix"/> or <see cref="RemovePrefix"/> to modify the set incrementally.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
    public IReadOnlyCollection<string> Prefixes //=> Volatile.Read(ref _prefixes);
    {
        get => Volatile.Read(ref _prefixes);
        set
        {
            ArgumentNullException.ThrowIfNull(value, nameof(value));
            var snapshot = ImmutableHashSet.CreateRange(StringComparer.OrdinalIgnoreCase, value.Select(p => p.NormalizePrefix()));
            Volatile.Write(ref _prefixes, snapshot);
        }
    }

    /// <summary>
    /// Removes a URL prefix from the listener's prefix collection.
    /// </summary>
    /// <param name="prefix">The prefix to remove. The value is normalized before comparison.</param>
    /// <remarks>
    /// This method can be called while the listener is running.  
    /// If the specified prefix does not exist (case-insensitive), the call has no effect.  
    /// Prefix normalization is performed using the same rules and case-insensitive comparer  
    /// (<see cref="StringComparer.OrdinalIgnoreCase"/>) as the routing system.
    /// </remarks>
    public void RemovePrefix(string prefix)
    {
        ImmutableInterlocked.Update(ref _prefixes, p => p.Remove(prefix.NormalizePrefix()));
    }

    /// <summary>
    /// If true, the listener will request and require a client certificate during the TLS handshake.
    /// </summary>
    /// <remarks>
    /// This enables (and requires) mTLS for the listener.
    /// </remarks>
    public bool RequireClientCertificate
    {
        get => _requireClientCertificate;
        set
        {
            ThrowIfSealed();
            _requireClientCertificate = value;
        }
    }

    /// <summary>
    /// Gets a value indicating whether TLS is enabled for this listener.
    /// </summary>
    /// <remarks>
    /// Returns true when a valid <see cref="Certificate"/> is provided.
    /// </remarks>
    public bool TlsEnabled => _certificate != null;

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
    public ITraceIdentifierProvider TraceIdentifierProvider
    {
        get => _traceIdentifierProvider;
        set => _traceIdentifierProvider = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Gets a value indicating whether this <see cref="ListenerOptions"/> instance is sealed.
    /// </summary>
    /// <remarks>
    /// When sealed, properties that affect the listener's binding or handshake behavior cannot be
    /// modified. These properties include:
    /// <list type="bullet">
    /// <item><see cref="BindAddress"/></item>
    /// <item><see cref="Port"/></item>
    /// <item><see cref="CheckCertificateRevocation"/></item>
    /// <item><see cref="RequireClientCertificate"/></item>
    /// </list>
    /// All other properties remain mutable.
    /// </remarks>
    internal bool IsSealed => _isSealed;

    /// <summary>
    /// Marks this <see cref="ListenerOptions"/> instance as sealed, preventing modification  
    /// of properties that are not safe to change while the listener is running.
    /// </summary>
    /// <remarks>
    /// This method is called internally when the listener starts, to lock in values that  
    /// affect socket binding, TLS configuration, and other connection-level behaviors.
    /// </remarks>
    internal void Seal()
    {
        _isSealed = true;
    }

    /// <summary>
    /// Marks this <see cref="ListenerOptions"/> instance as unsealed, allowing all properties  
    /// to be modified again.
    /// </summary>
    /// <remarks>
    /// This method is called internally when the listener stops, to permit configuration  
    /// changes before the next start.
    /// </remarks>
    internal void Unseal()
    {
        _isSealed = false;
    }

    /// <summary>
    /// Throws an <see cref="InvalidOperationException"/> if this instance is sealed.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    private void ThrowIfSealed()
    {
        if (_isSealed) throw new InvalidOperationException(MessageOptionsAreSealed);
    }
}
