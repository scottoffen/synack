namespace Synack;

/// <summary>
/// Behavior used when the request queue is at capacity.
/// </summary>
public enum QueueOverflowMode
{
    /// <summary>
    /// Wait for space to become available in the queue before accepting the request.
    /// </summary>
    Wait,

    /// <summary>
    /// Reject the incoming request immediately (return HTTP 503 Service Unavailable).
    /// </summary>
    Drop
}
