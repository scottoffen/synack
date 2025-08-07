namespace Synack.Handlers;

/// <summary>
/// Represents a connection handler that listens for incoming connections and dispatches parsed HTTP contexts.
/// </summary>
internal interface IConnectionHandler
{
    /// <summary>
    /// Gets a value indicating whether the handler is currently running.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Gets the port on which the handler is actively listening for incoming connections.
    /// </summary>
    /// <remarks>
    /// This reflects the actual port bound by the underlying TCP listener.  
    /// If <see cref="ListenerOptions.Port"/> was set to <c>0</c>, the operating system selects a free ephemeral port at runtime.  
    /// In such cases, this property returns the dynamically assigned port once the handler has started.
    ///
    /// Attempting to access this property before the handler has been started may return an undefined or placeholder value,
    /// depending on the implementation. For accurate results, ensure <see cref="StartAsync"/> has completed.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the handler is not running when the property is accessed.
    /// </exception>
    int Port { get; }

    /// <summary>
    /// Sets the dispatcher callback that will be invoked with each parsed <see cref="IHttpContext"/>.
    /// </summary>
    /// <param name="dispatcher">The delegate used to process incoming HTTP contexts.</param>
    void SetDispatcher(Func<IHttpContext, Task> dispatcher);

    /// <summary>
    /// Starts listening for incoming connections.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops listening and shuts down the connection handler.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task StopAsync(CancellationToken cancellationToken = default);
}
