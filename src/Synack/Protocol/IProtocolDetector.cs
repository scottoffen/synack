namespace Synack.Protocol;

/// <summary>
/// Detects the protocol version from the initial bytes of a stream.
/// </summary>
public interface IProtocolDetector
{
    /// <summary>
    /// Detects the protocol version from the initial bytes of the stream.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="buffer">The buffer to store the read bytes.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The detected protocol version.</returns>
    Task<ProtocolVersion> DetectProtocolAsync(
        Stream stream,
        byte[] buffer,
        CancellationToken cancellationToken = default
    );
}
