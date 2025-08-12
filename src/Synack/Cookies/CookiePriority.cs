namespace Synack.Cookies;

/// <summary>
/// Indicates the cookie's delivery priority (Chromium).
/// </summary>
public enum CookiePriority
{
    /// <summary>
    /// Lowest priority; may be evicted or throttled first under resource pressure.
    /// </summary>
    Low,
    /// <summary>
    /// Default priority when unspecified.
    /// </summary>
    Medium,
    /// <summary>
    /// Highest priority; less likely to be evicted.
    /// </summary>
    High
}
