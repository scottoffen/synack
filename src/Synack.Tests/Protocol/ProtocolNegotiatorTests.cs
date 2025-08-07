using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using Synack.Protocol;

namespace Synack.Tests;

public class ProtocolNegotiatorTests
{
    [Fact]
    public async Task NegotiateAsync_ReturnsHttp1_WhenDetectorReturnsHttp1()
    {
        var detector = new Mock<IProtocolDetector>();
        detector.Setup(d => d.DetectProtocolAsync(It.IsAny<Stream>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ProtocolVersion.Http1);

        var negotiator = new ProtocolNegotiator(detector.Object, loggerFactory: null);
        var stream = new MemoryStream();

        var (resultStream, version) = await negotiator.NegotiateAsync(stream, cert: null);

        version.ShouldBe(ProtocolVersion.Http1);
        resultStream.ShouldBeOfType<PrependStream>();
    }

    [Fact]
    public async Task NegotiateAsync_ReturnsHttp2_WhenDetectorReturnsHttp2()
    {
        var detector = new Mock<IProtocolDetector>();
        detector.Setup(d => d.DetectProtocolAsync(It.IsAny<Stream>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ProtocolVersion.Http2);

        var negotiator = new ProtocolNegotiator(detector.Object, loggerFactory: null);
        var stream = new MemoryStream();

        var (resultStream, version) = await negotiator.NegotiateAsync(stream, cert: null);

        version.ShouldBe(ProtocolVersion.Http2);
        resultStream.ShouldBeOfType<PrependStream>();
    }

    [Fact]
    public async Task NegotiateAsync_ReturnsUnknown_WhenDetectorReturnsUnknown()
    {
        var detector = new Mock<IProtocolDetector>();
        detector.Setup(d => d.DetectProtocolAsync(It.IsAny<Stream>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ProtocolVersion.Unknown);

        var negotiator = new ProtocolNegotiator(detector.Object, loggerFactory: null);
        var stream = new MemoryStream();

        var (resultStream, version) = await negotiator.NegotiateAsync(stream, cert: null);

        version.ShouldBe(ProtocolVersion.Unknown);
        resultStream.ShouldBeOfType<PrependStream>();
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenProtocolDetectorIsNull()
    {
        Should.Throw<ArgumentNullException>(() =>
        {
            _ = new ProtocolNegotiator(null!, loggerFactory: null);
        });
    }

    [Theory]
    [InlineData("Http2", ProtocolVersion.Http2)]
    [InlineData("Http11", ProtocolVersion.Http1)]
    [Trait("Category", "Integration")]
    public async Task NegotiateAsync_ReturnsCorrectVersion_BasedOnAlpn(
    string alpnProtocol,
    ProtocolVersion expectedVersion)
    {
        var cert = TestCertificateFactory.Create("localhost");
        var detector = new Mock<IProtocolDetector>().Object;
        var negotiator = new ProtocolNegotiator(detector, loggerFactory: null);

        using var listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();

        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;

        var serverTask = Task.Run(async () =>
        {
            using var serverClient = await listener.AcceptTcpClientAsync();
            var serverStream = serverClient.GetStream();

            var (stream, version) = await negotiator.NegotiateAsync(serverStream, cert);

            version.ShouldBe(expectedVersion);
            stream.ShouldBeOfType<SslStream>();
        });

        using var client = new TcpClient();
        await client.ConnectAsync("localhost", port);

        using var clientStream = new SslStream(
            client.GetStream(),
            leaveInnerStreamOpen: false,
            userCertificateValidationCallback: (_, _, _, _) => true);

        var protocol = alpnProtocol switch
        {
            "Http2" => SslApplicationProtocol.Http2,
            "Http11" => SslApplicationProtocol.Http11,
            _ => throw new ArgumentOutOfRangeException(nameof(alpnProtocol))
        };

        var clientOptions = new SslClientAuthenticationOptions
        {
            TargetHost = "localhost",
            ApplicationProtocols = [protocol],
            EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
        };

        await clientStream.AuthenticateAsClientAsync(clientOptions);
        await serverTask;
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task NegotiateAsync_ReturnsUnknown_WhenClientDoesNotSendAlpn()
    {
        var cert = TestCertificateFactory.Create("localhost");
        var detector = new Mock<IProtocolDetector>().Object;
        var negotiator = new ProtocolNegotiator(detector, loggerFactory: null);

        using var listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();

        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;

        var serverTask = Task.Run(async () =>
        {
            using var serverClient = await listener.AcceptTcpClientAsync();
            var serverStream = serverClient.GetStream();

            var (stream, version) = await negotiator.NegotiateAsync(serverStream, cert);

            version.ShouldBe(ProtocolVersion.Unknown);
            stream.ShouldBeOfType<SslStream>();

            var sslStream = (SslStream)stream;
            sslStream.NegotiatedApplicationProtocol.Protocol.Length.ShouldBe(0);
        });

        using var client = new TcpClient();
        await client.ConnectAsync("localhost", port);

        using var clientStream = new SslStream(
            client.GetStream(),
            leaveInnerStreamOpen: false,
            userCertificateValidationCallback: (_, _, _, _) => true);

        // Don't set ApplicationProtocols at all
        var clientOptions = new SslClientAuthenticationOptions
        {
            TargetHost = "localhost",
            EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
        };

        await clientStream.AuthenticateAsClientAsync(clientOptions);
        await serverTask;
    }
}
