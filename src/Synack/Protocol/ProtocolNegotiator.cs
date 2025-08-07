using System.Net.Security;
using Microsoft.Extensions.Logging;
using Synack.Streams;

namespace Synack.Protocol;

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
    /// <param name="token"></param>
    /// <returns></returns>
    public async Task<(Stream stream, ProtocolVersion version)> NegotiateAsync(
        Stream stream,
        CancellationToken token = default
    )
    {
        if (stream is not SslStream sslStream)
        {
            var buffer = new byte[ProtocolDetector.MaxInitialBytes];
            var detectedVersion = await _protocolDetector.DetectProtocolAsync(stream, buffer, token);
            var prependStream = new PrependStream(buffer, stream);
            return (prependStream, detectedVersion);
        }

        var negotiated = sslStream.NegotiatedApplicationProtocol;
        var version = negotiated switch
        {
            var p when p == SslApplicationProtocol.Http2 => ProtocolVersion.Http2,
            var p when p == SslApplicationProtocol.Http11 => ProtocolVersion.Http1,
            _ => ProtocolVersion.Unknown
        };

        return (sslStream, version);
    }
}
