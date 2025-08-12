using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Synack.Handlers;
using Synack.Protocol;

namespace Synack.Tests.Handlers;

[Trait("Category", "Integration")]
public sealed class TcpConnectionHandlerIntegrationTests
{
    private class FakeNegotiator : IProtocolNegotiator
    {
        public ProtocolVersion VersionToReturn { get; set; } = ProtocolVersion.Http1;

        public Task<(Stream, ProtocolVersion)> NegotiateAsync(Stream stream, CancellationToken token)
        {
            return Task.FromResult<(Stream, ProtocolVersion)>((stream, VersionToReturn));
        }
    }

    [Fact]
    public async Task Start_Stop_Should_Set_IsRunning_Properly()
    {
        var options = new ListenerOptions();
        var handler = new TcpConnectionHandler(options, new FakeNegotiator(), NullLogger<TcpConnectionHandler>.Instance);
        handler.SetDispatcher(_ => Task.CompletedTask);

        handler.IsRunning.ShouldBeFalse();
        await handler.StartAsync();
        handler.IsRunning.ShouldBeTrue();

        await handler.StopAsync();
        handler.IsRunning.ShouldBeFalse();
    }

    [Fact]
    public async Task StartAsync_InvokesDispatcher_OnIncomingConnection()
    {
        var dispatcherCalled = new TaskCompletionSource<IHttpContext>();
        var dispatcher = new Func<IHttpContext, Task>(ctx =>
        {
            dispatcherCalled.TrySetResult(ctx);
            return Task.CompletedTask;
        });

        var options = new ListenerOptions { Port = 0 };
        var handler = new TcpConnectionHandler(options, new FakeNegotiator(), NullLogger<TcpConnectionHandler>.Instance);
        handler.SetDispatcher(dispatcher);

        await handler.StartAsync();

        var port = handler.Port;

        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, port);
        await using var stream = client.GetStream();

        // Send garbage data to force negotiation and trigger dispatcher
        var data = new byte[] { 0x48, 0x54, 0x54, 0x50 }; // "HTTP" to simulate request start
        await stream.WriteAsync(data);

        // Wait up to 2 seconds for dispatcher to be called
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var result = await dispatcherCalled.Task.WaitAsync(cts.Token);
        result.ShouldNotBeNull();

        await handler.StopAsync();
    }

    [Fact]
    public async Task Dispatcher_IsInvoked_WhenClientConnects()
    {
        var called = false;

        var options = new ListenerOptions
        {
            BindAddress = IPAddress.Loopback
        };

        var negotiator = new FakeNegotiator();
        var handler = new TcpConnectionHandler(options, negotiator, NullLogger<TcpConnectionHandler>.Instance);
        handler.SetDispatcher(_ =>
        {
            called = true;
            return Task.CompletedTask;
        });

        await handler.StartAsync();
        var port = handler.Port;

        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, port);

        // Simulate minimal request to trigger negotiator
        var stream = client.GetStream();
        var data = Encoding.ASCII.GetBytes("GET / HTTP/1.1\r\nHost: localhost\r\n\r\n");
        await stream.WriteAsync(data);
        await stream.FlushAsync();

        // Allow server time to dispatch
        await Task.Delay(250);

        // Assert
        called.ShouldBeTrue();

        await handler.StopAsync();
    }

    [Fact]
    public async Task Dispatcher_IsInvoked_AfterTlsNegotiation()
    {
        var called = false;

        var options = new ListenerOptions
        {
            BindAddress = IPAddress.Loopback,
            Certificate = TestCertificateFactory.Create("localhost")
        };

        var negotiator = new FakeNegotiator();
        var handler = new TcpConnectionHandler(options, negotiator, NullLogger<TcpConnectionHandler>.Instance);

        handler.SetDispatcher(_ =>
        {
            called = true;
            return Task.CompletedTask;
        });

        await handler.StartAsync();
        var port = handler.Port;

        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, port);

        using var ssl = new SslStream(client.GetStream(), false, (_, _, _, _) => true);
        await ssl.AuthenticateAsClientAsync("localhost");

        var data = Encoding.ASCII.GetBytes("GET / HTTP/1.1\r\nHost: localhost\r\n\r\n");
        await ssl.WriteAsync(data);
        await ssl.FlushAsync();

        await Task.Delay(250);

        called.ShouldBeTrue();
        await handler.StopAsync();
    }

    [Fact]
    public async Task Dispatcher_IsNotCalled_WhenProtocolIsUnknown()
    {
        var called = false;

        var options = new ListenerOptions
        {
            BindAddress = IPAddress.Loopback,
        };

        var negotiator = new FakeNegotiator
        {
            VersionToReturn = ProtocolVersion.Unknown
        };

        var handler = new TcpConnectionHandler(options, negotiator, NullLogger<TcpConnectionHandler>.Instance);

        handler.SetDispatcher(_ =>
        {
            called = true;
            return Task.CompletedTask;
        });

        await handler.StartAsync();
        var port = handler.Port;

        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, port);

        using var stream = client.GetStream();
        var data = Encoding.ASCII.GetBytes("UNKNOWN\r\n");
        await stream.WriteAsync(data);
        await stream.FlushAsync();

        await Task.Delay(250);

        called.ShouldBeFalse();
        await handler.StopAsync();
    }

    [Fact]
    public async Task Dispatcher_Exception_DoesNotCrashHandler()
    {
        var options = new ListenerOptions
        {
            BindAddress = IPAddress.Loopback
        };

        var negotiator = new FakeNegotiator();

        var handler = new TcpConnectionHandler(options, negotiator, NullLogger<TcpConnectionHandler>.Instance);

        handler.SetDispatcher(_ => throw new InvalidOperationException("Intentional failure"));

        await handler.StartAsync();
        var port = handler.Port;

        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, port);

        using var stream = client.GetStream();
        var data = Encoding.ASCII.GetBytes("GET / HTTP/1.1\r\nHost: localhost\r\n\r\n");
        await stream.WriteAsync(data);
        await stream.FlushAsync();

        // Give the handler a moment to process the request
        await Task.Delay(250);

        handler.IsRunning.ShouldBeTrue();

        await handler.StopAsync();
    }
}
