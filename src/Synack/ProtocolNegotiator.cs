using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;

namespace Synack;

/// <summary>
/// Negotiates the protocol version for a given stream.
/// </summary>
public interface IProtocolNegotiator
{
    /// <summary>
    /// Negotiates the protocol version for the given stream.
    /// </summary>
    /// <param name="stream">The stream to negotiate.</param>
    /// <param name="cert">The server certificate for SSL/TLS negotiation, if applicable.</param>
    /// <param name="token">Cancellation token to cancel the operation.</param>
    /// <returns>A tuple containing the negotiated stream and protocol version.</returns>
    Task<(Stream stream, ProtocolVersion version)> NegotiateAsync(
        Stream stream,
        X509Certificate2? cert,
        CancellationToken token = default
    );
}

internal sealed class ProtocolNegotiator : IProtocolNegotiator
{
    private readonly ILoggerFactory? _loggerFactory;
    private readonly IProtocolDetector _protocolDetector;

    public ProtocolNegotiator(
        IProtocolDetector protocolDetector,
        ILoggerFactory? loggerFactory
    )
    {
        _protocolDetector = protocolDetector
            ?? throw new ArgumentNullException(nameof(protocolDetector));

        _loggerFactory = loggerFactory;
    }

    /// <summary>
    /// Negotiates the protocol version for the given stream.
    /// </summary>
    /// <remarks>
    /// If a certificate is provided, it will use SSL/TLS negotiation.
    /// If no certificate is provided, it will attempt to detect the protocol version
    /// by reading the initial bytes from the stream.
    /// </remarks>
    /// <param name="stream"></param>
    /// <param name="cert"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public async Task<(Stream stream, ProtocolVersion version)> NegotiateAsync(
        Stream stream,
        X509Certificate2? cert,
        CancellationToken token = default
    )
    {
        var version = ProtocolVersion.Unknown;

        if (cert is null)
        {
            var buffer = new byte[ProtocolDetector.MaxInitialBytes];
            version = await _protocolDetector.DetectProtocolAsync(stream, buffer, token);
            var prependStream = new PrependStream(buffer, stream);
            return (prependStream, version);
        }

        var sslStream = new SslStream(stream, leaveInnerStreamOpen: false);
        var options = new SslServerAuthenticationOptions
        {
            ServerCertificate = cert,
            EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
            ApplicationProtocols =
            [
                SslApplicationProtocol.Http2,
                SslApplicationProtocol.Http11
            ]
        };

        await sslStream.AuthenticateAsServerAsync(options, token);
        var negotiated = sslStream.NegotiatedApplicationProtocol;

        version = negotiated switch
        {
            var p when p == SslApplicationProtocol.Http2 => ProtocolVersion.Http2,
            var p when p == SslApplicationProtocol.Http11 => ProtocolVersion.Http1,
            _ => ProtocolVersion.Unknown
        };

        return (sslStream, version);
    }
}
