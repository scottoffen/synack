using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging.Abstractions;
using Synack.Handlers;
using Synack.Protocol;

namespace Synack.Tests.Handlers;

public class TcpConnectionHandlerUnitTests
{
    private readonly ListenerOptions _options = new() { Port = 0 };
    private readonly Mock<IProtocolNegotiator> _negotiator = new();

    [Fact]
    public void Constructor_Throws_WhenOptionsIsNull()
    {
        Should.Throw<ArgumentNullException>(() =>
            new TcpConnectionHandler(null!, _negotiator.Object, NullLogger<TcpConnectionHandler>.Instance));
    }

    [Fact]
    public void Constructor_Throws_WhenNegotiatorIsNull()
    {
        Should.Throw<ArgumentNullException>(() =>
            new TcpConnectionHandler(_options, null!, NullLogger<TcpConnectionHandler>.Instance));
    }

    [Fact]
    public void SetDispatcher_Throws_WhenNull()
    {
        var handler = CreateHandler();
        Should.Throw<ArgumentNullException>(() =>
            handler.SetDispatcher(null!));
    }

    [Fact]
    public async Task StartAsync_Throws_WhenDispatcherNotSet()
    {
        var handler = CreateHandler();
        await Should.ThrowAsync<InvalidOperationException>(() =>
            handler.StartAsync());
    }

    [Fact]
    public async Task StartAsync_SetsIsRunning()
    {
        var handler = CreateHandler();
        handler.SetDispatcher(_ => Task.CompletedTask);

        await handler.StartAsync();

        handler.IsRunning.ShouldBeTrue();
        await handler.StopAsync();
    }

    [Fact]
    public async Task StopAsync_ResetsIsRunning()
    {
        var handler = CreateHandler();
        handler.SetDispatcher(_ => Task.CompletedTask);

        await handler.StartAsync();
        await handler.StopAsync();

        handler.IsRunning.ShouldBeFalse();
    }

    [Fact]
    public async Task StartAsync_DoesNothing_WhenAlreadyRunning()
    {
        var handler = CreateHandler();
        handler.SetDispatcher(_ => Task.CompletedTask);

        await handler.StartAsync();
        await handler.StartAsync(); // second call should be no-op

        handler.IsRunning.ShouldBeTrue();
        await handler.StopAsync();
    }

    [Fact]
    public async Task StopAsync_DoesNothing_WhenNotRunning()
    {
        var handler = CreateHandler();
        handler.SetDispatcher(_ => Task.CompletedTask);

        await handler.StopAsync(); // should not throw
        handler.IsRunning.ShouldBeFalse();
    }

    [Fact]
    public void Port_ThrowsInvalidOperationException_WhenHandlerNotRunning()
    {
        var options = new ListenerOptions { Port = 12345 };
        var negotiator = Mock.Of<IProtocolNegotiator>();

        var handler = new TcpConnectionHandler(options, negotiator, NullLogger<TcpConnectionHandler>.Instance);

        var exception = Should.Throw<InvalidOperationException>(() =>
        {
            var _ = handler.Port;
        });

        exception.Message.ShouldBe("Handler must be started to retrieve the bound port.");
    }

    [Fact]
    public async Task Port_ReturnsConfiguredPort_WhenStaticPortUsed()
    {
        var port = GetFreePort();
        var options = new ListenerOptions { Port = port };
        var negotiator = Mock.Of<IProtocolNegotiator>();
        var handler = new TcpConnectionHandler(options, negotiator, NullLogger<TcpConnectionHandler>.Instance);

        handler.SetDispatcher(_ => Task.CompletedTask);

        await handler.StartAsync();
        var actualPort = handler.Port;
        await handler.StopAsync();

        actualPort.ShouldBe(port);
    }

    [Fact]
    public async Task Port_ReturnsAssignedPort_WhenDynamicPortUsed()
    {
        var options = new ListenerOptions { Port = 0 };
        var negotiator = Mock.Of<IProtocolNegotiator>();
        var handler = new TcpConnectionHandler(options, negotiator, NullLogger<TcpConnectionHandler>.Instance);

        handler.SetDispatcher(_ => Task.CompletedTask);

        await handler.StartAsync();
        var actualPort = handler.Port;
        await handler.StopAsync();

        actualPort.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task StartAsync_ThrowsSocketException_WhenPortIsUnavailable()
    {
        var inUsePort = GetFreePort();
        using var tempListener = new TcpListener(IPAddress.Any, inUsePort);
        tempListener.Start();
        tempListener.Server.IsBound.ShouldBeTrue();

        var options = new ListenerOptions { Port = inUsePort };
        var negotiator = Mock.Of<IProtocolNegotiator>();
        var handler = new TcpConnectionHandler(options, negotiator, NullLogger<TcpConnectionHandler>.Instance);
        handler.SetDispatcher(_ => Task.CompletedTask);

        var ex = await Should.ThrowAsync<SocketException>(() => handler.StartAsync());

        tempListener.Stop();

        ex.SocketErrorCode.ShouldBe(SocketError.AddressAlreadyInUse);
    }

    private static int GetFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private TcpConnectionHandler CreateHandler() =>
        new(_options, _negotiator.Object, NullLogger<TcpConnectionHandler>.Instance);
}
