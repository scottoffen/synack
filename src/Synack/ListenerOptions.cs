using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography.X509Certificates;
using Synack.Certificates;
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
    /// Gets or sets the X.509 certificate used for TLS encryption.
    /// Setting this property validates the certificate.
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
    /// Gets or sets an optional name identifying this listener.
    /// </summary>
    public string? Name { get; set; }

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
    /// This is only applicable for mTLS (mutual TLS) scenarios.
    /// </remarks>
    public bool RequireClientCertificate { get; set; } = false;

    /// <summary>
    /// Gets or sets a collection of tags associated with this listener.
    /// </summary>
    public List<string> Tags { get; set; } = [];
}
