namespace Synack;

/// <summary>
/// Represents the protocol version used in the http request
/// </summary>
public enum ProtocolVersion
{
    /// <summary>
    /// Unknown protocol version, used when the version is not specified or recognized.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// HTTP/1.* protocol versions
    /// </summary>
    Http1,

    /// <summary>
    /// HTTP/2 protocol version
    /// </summary>
    Http2
}
