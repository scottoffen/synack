using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Synack.Streams;

namespace Synack.Tests.Streams;

[Trait("Category", "Integration")]
public class TlsStreamFactoryTests
{
    [Fact]
    public async Task AuthenticateAsync_Succeeds_WhenClientCertNotRequired()
    {
        var listenerOptions = new ListenerOptions
        {
            Certificate = TestCertificateFactory.Create("localhost"),
            RequireClientCertificate = false
        };

        var factory = new TlsStreamFactory(listenerOptions);

        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();

        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var serverTask = Task.Run(async () =>
        {
            using var serverClient = await listener.AcceptTcpClientAsync();
            using var rawStream = serverClient.GetStream();

            var sslStream = await factory.AuthenticateAsync(rawStream, CancellationToken.None);
            sslStream.ShouldBeOfType<SslStream>();
            sslStream.IsAuthenticated.ShouldBeTrue();
        });

        using var client = new TcpClient();
        await client.ConnectAsync("localhost", port);

        using var clientStream = new SslStream(
            client.GetStream(),
            leaveInnerStreamOpen: false,
            userCertificateValidationCallback: (_, _, _, _) => true);

        var clientOptions = new SslClientAuthenticationOptions
        {
            TargetHost = "localhost",
            EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
            ApplicationProtocols = [SslApplicationProtocol.Http11]
        };

        await clientStream.AuthenticateAsClientAsync(clientOptions);
        await serverTask;
    }

    [Fact]
    public async Task AuthenticateAsync_Fails_WhenClientCertRequired_AndNotProvided()
    {
        var listenerOptions = new ListenerOptions
        {
            Certificate = TestCertificateFactory.Create("localhost"),
            RequireClientCertificate = true
        };

        var factory = new TlsStreamFactory(listenerOptions);

        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();

        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var serverTask = Task.Run(async () =>
        {
            using var serverClient = await listener.AcceptTcpClientAsync();
            using var rawStream = serverClient.GetStream();

            await Should.ThrowAsync<AuthenticationException>(async () =>
            {
                await factory.AuthenticateAsync(rawStream, CancellationToken.None);
            });
        });

        using var client = new TcpClient();
        await client.ConnectAsync("localhost", port);

        using var clientStream = new SslStream(
            client.GetStream(),
            leaveInnerStreamOpen: false,
            userCertificateValidationCallback: (_, _, _, _) => true);

        var clientOptions = new SslClientAuthenticationOptions
        {
            TargetHost = "localhost",
            EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
            ApplicationProtocols = [SslApplicationProtocol.Http11]
        };

        await clientStream.AuthenticateAsClientAsync(clientOptions);
        await serverTask;
    }

    [Fact]
    public async Task AuthenticateAsync_Succeeds_WhenClientCertIsValid()
    {
        var clientCert = TestCertificateFactory.Create("client", includePrivateKey: true);
        var listenerOptions = new ListenerOptions
        {
            Certificate = TestCertificateFactory.Create("localhost"),
            RequireClientCertificate = true,
            ClientCertificateValidator = cert => cert.Subject.Contains("CN=client")
        };

        var factory = new TlsStreamFactory(listenerOptions);

        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();

        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var serverTask = Task.Run(async () =>
        {
            using var serverClient = await listener.AcceptTcpClientAsync();
            using var rawStream = serverClient.GetStream();
            var sslStream = await factory.AuthenticateAsync(rawStream, CancellationToken.None);

            sslStream.IsAuthenticated.ShouldBeTrue();
            sslStream.RemoteCertificate.ShouldNotBeNull();
        });

        using var client = new TcpClient();
        await client.ConnectAsync("localhost", port);

        using var clientStream = new SslStream(client.GetStream(), false, (_, _, _, _) => true);

        var clientOptions = new SslClientAuthenticationOptions
        {
            TargetHost = "localhost",
            ClientCertificates = new X509CertificateCollection { clientCert },
            EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
            ApplicationProtocols = [SslApplicationProtocol.Http11]
        };

        await clientStream.AuthenticateAsClientAsync(clientOptions);
        await serverTask;
    }
}
