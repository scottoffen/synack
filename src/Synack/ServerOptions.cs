using System.Collections.ObjectModel;

namespace Synack;

/// <summary>
/// Represents configuration settings for a Synack server instance.
/// </summary>
public sealed class ServerOptions
{
    internal static readonly string MessageConcurrencyCannotBeZero = $"{nameof(MaxConcurrentRequests)} cannot be zero.";
    internal static readonly string MessageQueueCapacityCannotBeZero = $"{nameof(MaxQueueCapacity)} cannot be zero.";
    internal static readonly string MessageOptionsAreSealed = $"{nameof(ServerOptions)} cannot be modified while the server is running.";
    internal static readonly string MessageTimeoutMustBeNonNegativeOrInfinite = $"{nameof(RequestQueueTimeout)} must be non-negative or Timeout.InfiniteTimeSpan.";

    private readonly List<ListenerOptions> _listeners = [];
    private int _maxConcurrentRequests = -1;
    private int _maxQueueCapacity = 1024;
    private QueueOverflowMode _queueOverflowMode = QueueOverflowMode.Wait;
    private TimeSpan _requestQueueTimeout = TimeSpan.FromSeconds(30);

    private readonly ReadOnlyCollection<ListenerOptions> _listenersReadOnly;

    public ServerOptions()
    {
        _listenersReadOnly = _listeners.AsReadOnly();
    }

    private volatile bool _isSealed;

    /// <summary>
    /// Gets a value indicating whether the server options have been sealed (made immutable).
    /// </summary>
    internal bool IsSealed => _isSealed;

    /// <summary>
    /// Gets the collection of listeners configured for this server instance.
    /// </summary>
    public ReadOnlyCollection<ListenerOptions> Listeners => _listeners.AsReadOnly();

    /// <summary>
    /// Gets or sets the maximum number of requests that may execute concurrently.
    /// Use -1 for unbounded. Default is -1 (unbounded).
    /// </summary>
    /// <remarks>
    /// Setting this to 0 throws <see cref="ArgumentOutOfRangeException"/>.
    /// </remarks>
    public int MaxConcurrentRequests
    {
        get => _maxConcurrentRequests;
        set
        {
            ThrowIfSealed();

            if (value == 0)
                throw new ArgumentOutOfRangeException(nameof(MaxConcurrentRequests), value, MessageConcurrencyCannotBeZero);

            _maxConcurrentRequests = value < 0 ? -1 : value;
        }
    }

    /// <summary>
    /// Gets or sets the maximum number of requests allowed to wait in the internal queue.
    /// Use -1 for unbounded. Default is 1024.
    /// </summary>
    /// <remarks>
    /// Setting this to 0 throws <see cref="ArgumentOutOfRangeException"/>.
    /// This controls backpressure before request execution begins.
    /// </remarks>
    public int MaxQueueCapacity
    {
        get => _maxQueueCapacity;
        set
        {
            ThrowIfSealed();

            if (value == 0)
                throw new ArgumentOutOfRangeException(nameof(MaxQueueCapacity), value, MessageQueueCapacityCannotBeZero);

            _maxQueueCapacity = value < 0 ? -1 : value;
        }
    }

    /// <summary>
    /// Behavior when the request queue is full (applies only when <see cref="MaxQueueCapacity"/> is bounded).
    /// Default is <see cref="QueueOverflowMode.Wait"/>.
    /// </summary>
    /// <remarks>
    /// This setting only takes effect when <see cref="MaxQueueCapacity"/> is set to a bounded value (not -1).
    /// </remarks>
    public QueueOverflowMode QueueOverflowMode
    {
        get => _queueOverflowMode;
        set
        {
            ThrowIfSealed();
            _queueOverflowMode = value;
        }
    }

    /// <summary>
    /// The maximum time to wait for space in the queue when <see cref="QueueOverflowMode"/> is <see cref="QueueOverflowMode.Wait"/>.
    /// Use <see cref="Timeout.InfiniteTimeSpan"/> to wait indefinitely. Default is 00:00:30.
    /// </summary>
    /// <remarks>
    /// This timeout is only applicable when <see cref="QueueOverflowMode"/> is set to <see cref="QueueOverflowMode.Wait"/>
    /// and <see cref="MaxQueueCapacity"/> is bounded (not -1).
    /// </remarks>
    public TimeSpan RequestQueueTimeout
    {
        get => _requestQueueTimeout;
        set
        {
            ThrowIfSealed();

            if (value < TimeSpan.Zero && value != Timeout.InfiniteTimeSpan)
                throw new ArgumentOutOfRangeException(nameof(RequestQueueTimeout), value, MessageTimeoutMustBeNonNegativeOrInfinite);

            _requestQueueTimeout = value;
        }
    }

    /// <summary>
    /// Adds a new listener to the server configuration.
    /// </summary>
    /// <param name="listener">The listener options to add.</param>
    /// <exception cref="ArgumentNullException">Thrown if the listener is null.</exception>
    public void AddListener(ListenerOptions listener)
    {
        ThrowIfSealed();

        ArgumentNullException.ThrowIfNull(listener);
        _listeners.Add(listener);
    }

    /// <summary>
    /// Seals the options, preventing further modifications.
    /// </summary>
    internal void Seal()
    {
        if (_isSealed) return;

        foreach (var listener in _listeners)
        {
            listener.Seal();
        }

        _isSealed = true;
    }

    /// <summary>
    /// Unseals the options, allowing modifications.
    /// </summary>
    internal void Unseal()
    {
        if (!_isSealed) return;

        foreach (var listener in _listeners)
        {
            listener.Unseal();
        }

        _isSealed = false;
    }

    private void ThrowIfSealed()
    {
        if (_isSealed) throw new InvalidOperationException(MessageOptionsAreSealed);
    }

    /// <summary>
    /// Creates a new options instance with production-friendly defaults.
    /// </summary>
    public static ServerOptions CreateInstance()
    {
        var options = new ServerOptions
        {
            // Keep concurrency unbounded by default; rely on queue capacity for backpressure.
            MaxConcurrentRequests = -1,
            MaxQueueCapacity = 1024,
            QueueOverflowMode = QueueOverflowMode.Wait,
            RequestQueueTimeout = TimeSpan.FromSeconds(30),
        };

        options.AddListener(new ListenerOptions
        {
            Port = 5000,
            Prefixes = ["/"]
        });

        return options;
    }
}
