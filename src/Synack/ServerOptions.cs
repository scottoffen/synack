using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;

namespace Synack;

/// <summary>
/// Represents configuration settings for a Synack server instance.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ServerOptions
{
    /// <summary>
    /// Gets the collection of listeners configured for this server instance.
    /// </summary>
    public List<ListenerOptions> Listeners { get; private set; } = [];

    /// <summary>
    /// Gets or sets the maximum number of concurrent requests the server can handle.
    /// Default is 100.
    /// </summary>
    public int MaxConcurrentRequests { get; set; } = 100;

    /// <summary>
    /// Gets or sets an optional name identifying this server instance.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the behavior when the request queue is full.
    /// Default is <see cref="BoundedChannelFullMode.Wait"/>.
    /// </summary>
    public BoundedChannelFullMode QueueOverflowMode { get; set; } = BoundedChannelFullMode.Wait;

    /// <summary>
    /// Gets or sets the optional timeout for requests waiting in the queue.
    /// </summary>
    public TimeSpan? RequestQueueTimeout { get; set; } = null;

    /// <summary>
    /// Gets or sets a collection of tags associated with this server instance.
    /// </summary>
    public List<string> Tags { get; set; } = [];

    internal static ServerOptions Default { get; } = new ServerOptions
    {
        MaxConcurrentRequests = 100,
        Name = "Default Server",
        QueueOverflowMode = BoundedChannelFullMode.Wait,
        RequestQueueTimeout = TimeSpan.FromSeconds(30),
        Listeners =
        [
            new() {
                Port = 5000,
                Prefixes = ["/"]
            }
        ],
    };
}
