using System.Security.Cryptography.X509Certificates;

namespace Synack.Protocol;

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
