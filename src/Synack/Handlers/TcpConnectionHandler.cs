using System.Net;
using System.Net.Sockets;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Synack.Protocol;
using Synack.Streams;

namespace Synack.Handlers;

internal sealed class TcpConnectionHandler : IConnectionHandler
{
    private readonly ILogger<TcpConnectionHandler>? _logger;
    private readonly TcpListener _listener;
    private readonly ListenerOptions _options;
    private readonly IProtocolNegotiator _negotiator;
    private readonly TlsStreamFactory _tlsStreamFactory;
    private Func<IHttpContext, Task>? _dispatcher;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _acceptLoop;

    public TcpConnectionHandler(ListenerOptions options, IProtocolNegotiator negotiator, ILogger<TcpConnectionHandler> logger)
    {
        _options = options
            ?? throw new ArgumentNullException(nameof(options), "Listener options cannot be null.");

        _negotiator = negotiator
            ?? throw new ArgumentNullException(nameof(negotiator), "Protocol negotiator cannot be null.");

        _logger = logger;

        _tlsStreamFactory = new TlsStreamFactory(_options);

        _listener = new TcpListener(_options.BindAddress, _options.Port);
    }

    public bool IsRunning { get; private set; }

    public int Port
    {
        get
        {
            if (!IsRunning)
                throw new InvalidOperationException("Handler must be started to retrieve the bound port.");
            return ((IPEndPoint)_listener.LocalEndpoint).Port;
        }
    }

    public void SetDispatcher(Func<IHttpContext, Task> dispatcher)
    {
        _dispatcher = dispatcher
            ?? throw new ArgumentNullException(nameof(dispatcher), "Dispatcher cannot be null.");
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_dispatcher == null)
            throw new InvalidOperationException("Dispatcher must be set before starting the handler.");

        if (IsRunning) return;

        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _listener.Start();

        _acceptLoop = Task.Factory.StartNew(
            () => AcceptLoopAsync(_cancellationTokenSource.Token),
            _cancellationTokenSource.Token,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);

        IsRunning = true;
        await Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!IsRunning) return;

        _cancellationTokenSource?.Cancel();
        _listener.Stop();

        if (_acceptLoop is not null)
        {
            await _acceptLoop.ConfigureAwait(false);
        }

        IsRunning = false;
    }

    /// <summary>
    /// Continuously accepts incoming TCP connections and delegates handling to <see cref="HandleConnectionAsync"/>.
    /// </summary>
    /// <param name="cancellationToken">A token used to terminate the loop.</param>
    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var client = await _listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
                _ = Task.Run(() => HandleConnectionAsync(client, cancellationToken));
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error accepting TCP connection.");
            }
        }
    }

    /// <summary>
    /// Handles a single TCP connection: negotiates protocol, applies TLS (if enabled), and invokes the dispatcher with the parsed context.
    /// </summary>
    /// <param name="client">The accepted TCP client.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    private async Task HandleConnectionAsync(TcpClient client, CancellationToken cancellationToken)
    {
        try
        {
            using var tcp = client;
            await using var networkStream = client.GetStream();
            Stream stream = networkStream;

            if (_options.TlsEnabled)
            {
                stream = await _tlsStreamFactory
                    .AuthenticateAsync(stream, cancellationToken)
                    .ConfigureAwait(false);
            }

            var (negotiatedStream, version) = await _negotiator
                .NegotiateAsync(stream, cancellationToken)
                .ConfigureAwait(false);

            if (version == ProtocolVersion.Unknown)
            {
                var endpoint = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
                _logger?.LogDebug("Unsupported protocol version received from {ClientEndpoint}", endpoint);
                await negotiatedStream.DisposeAsync().ConfigureAwait(false);
                return;
            }

            await using var finalStream = negotiatedStream;

            IHttpContext context = version switch
            {
                ProtocolVersion.Http1 => new DummyHttpContext(),
                ProtocolVersion.Http2 => throw new NotImplementedException("HTTP/2 not yet implemented."),
                _ => throw new InvalidOperationException("Unexpected protocol version.")
            };

            await _dispatcher!.Invoke(context).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogCritical(ex, "Error handling TCP connection.");
        }
    }

    private class DummyHttpContext : IHttpContext
    {
        public EndPoint LocalEndPoint => throw new NotImplementedException();

        public EndPoint RemoteEndPoint => throw new NotImplementedException();

        public IHttpRequest Request => throw new NotImplementedException();

        public CancellationToken RequestAborted => throw new NotImplementedException();

        public IHttpResponse Response => throw new NotImplementedException();

        public string TraceIdentifier => throw new NotImplementedException();

        public ClaimsPrincipal User { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public void OnCompleted(Func<Task> callback)
        {
            throw new NotImplementedException();
        }
    }
}
