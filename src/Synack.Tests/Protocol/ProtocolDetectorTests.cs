using System.Text;
using Synack.Protocol;

namespace Synack.Tests;

public class ProtocolDetectorTests
{
    private static readonly IProtocolDetector _protocolDetector = new ProtocolDetector(null);

    [Fact]
    public async Task DetectProtocolAsync_ReturnsHttp2_WhenPrefaceMatchesExactly()
    {
        var buffer = new byte[ProtocolDetector.MaxInitialBytes];
        var stream = new MemoryStream(Encoding.ASCII.GetBytes("PRI * HTTP/2.0\r\n\r\nSM\r\n\r\n"));

        var result = await _protocolDetector.DetectProtocolAsync(stream, buffer);

        result.ShouldBe(ProtocolVersion.Http2);
    }

    [Fact]
    public async Task DetectProtocolAsync_ReturnsHttp1_WhenFirstByteIsHttp1Method()
    {
        var buffer = new byte[ProtocolDetector.MaxInitialBytes];
        var stream = new MemoryStream(Encoding.ASCII.GetBytes("GET / HTTP/1.1\r\nHost: example.com\r\n\r\n"));

        var result = await _protocolDetector.DetectProtocolAsync(stream, buffer);

        result.ShouldBe(ProtocolVersion.Http1);
    }

    [Fact]
    public async Task DetectProtocolAsync_ReturnsUnknown_WhenFirstByteIsNotRecognized()
    {
        var buffer = new byte[ProtocolDetector.MaxInitialBytes];
        var stream = new MemoryStream(Encoding.ASCII.GetBytes("Z /unknown\r\n\r\n"));

        var result = await _protocolDetector.DetectProtocolAsync(stream, buffer);

        result.ShouldBe(ProtocolVersion.Unknown);
    }

    [Fact]
    public async Task DetectProtocolAsync_ReturnsUnknown_WhenStreamIsEmpty()
    {
        var buffer = new byte[ProtocolDetector.MaxInitialBytes];
        var stream = new MemoryStream(Array.Empty<byte>());

        var result = await _protocolDetector.DetectProtocolAsync(stream, buffer);

        result.ShouldBe(ProtocolVersion.Unknown);
    }

    [Fact]
    public async Task DetectProtocolAsync_ReturnsUnknown_WhenBufferIsIncompleteAndFirstByteIsInvalid()
    {
        var buffer = new byte[ProtocolDetector.MaxInitialBytes];
        var stream = new MemoryStream(Encoding.ASCII.GetBytes("XYZ /test"));

        var result = await _protocolDetector.DetectProtocolAsync(stream, buffer);

        result.ShouldBe(ProtocolVersion.Unknown);
    }
}
