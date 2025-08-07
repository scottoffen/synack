using System.Text;
using Microsoft.Extensions.Logging;

namespace Synack;

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

internal sealed class ProtocolDetector : IProtocolDetector
{
    private static readonly byte[] _http2Preface = Encoding.ASCII.GetBytes("PRI * HTTP/2.0\r\n\r\nSM\r\n\r\n");

    /// <summary>
    /// Maximum number of initial bytes to read for protocol detection.
    /// </summary>
    public static readonly int MaxInitialBytes = 24;

    private readonly ILoggerFactory? _loggerFactory;

    public ProtocolDetector(ILoggerFactory? loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    /// <summary>
    /// Detects the protocol version from the initial bytes of the stream.
    /// </summary>
    /// <remarks>
    /// If the stream starts with the HTTP/2 preface, it returns Http2.
    /// If the first byte is a valid HTTP/1 method, it returns Http1.
    /// Otherwise, it returns Unknown.
    /// </remarks>
    /// <param name="stream"></param>
    /// <param name="buffer"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<ProtocolVersion> DetectProtocolAsync(
        Stream stream,
        byte[] buffer,
        CancellationToken cancellationToken = default
    )
    {
        var bytesRead = await ReadInitialBytes(stream, buffer, cancellationToken);

        if (bytesRead > 0)
        {
            if (StartsWithHttp2Preface(buffer))
                return ProtocolVersion.Http2;

            if (StartsWithHttp1FirstLetter(buffer))
                return ProtocolVersion.Http1;
        }

        return ProtocolVersion.Unknown;
    }

    /// <summary>
    /// Reads the initial bytes from the stream into the provided buffer.
    /// </summary>
    /// <remarks>
    /// This method will read up to MaxInitialBytes bytes or until the end of the stream is reached.
    /// </remarks>
    /// <param name="stream"></param>
    /// <param name="buffer"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    private static async Task<int> ReadInitialBytes(Stream stream, byte[] buffer, CancellationToken token)
    {
        var bytesRead = 0;

        while (bytesRead < MaxInitialBytes)
        {
            var read = await stream.ReadAsync(buffer, bytesRead, MaxInitialBytes - bytesRead, token);
            if (read == 0) break;
            bytesRead += read;
        }

        return bytesRead;
    }

    /// <summary>
    /// Returns true if the buffer starts with the HTTP/2 preface.
    /// </summary>
    /// <param name="buffer"></param>
    /// <returns></returns>
    private static bool StartsWithHttp2Preface(byte[] buffer)
    {
        if (buffer.Length < MaxInitialBytes) return false;

        for (var i = 0; i < MaxInitialBytes; i++)
        {
            if (buffer[i] != _http2Preface[i])
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Returns true if the first byte of the buffer is the first letter of a valid HTTP/1 method.
    /// </summary>
    /// <remarks>
    /// This is a simplified check and may not cover all cases.
    /// </remarks>
    /// <param name="bytes"></param>
    /// <returns></returns>
    private static bool StartsWithHttp1FirstLetter(byte[] bytes)
    {
        switch (bytes[0])
        {
            case (byte)'G':
            case (byte)'g': // GET
            case (byte)'P':
            case (byte)'p': // POST, PUT, PATCH
            case (byte)'D':
            case (byte)'d': // DELETE
            case (byte)'H':
            case (byte)'h': // HEAD
            case (byte)'O':
            case (byte)'o': // OPTIONS
            case (byte)'C':
            case (byte)'c': // CONNECT
            case (byte)'T':
            case (byte)'t': // TRACE
                return true;
            default:
                return false;
        }
    }
}
