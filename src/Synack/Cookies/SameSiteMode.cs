namespace Synack.Cookies;

/// <summary>
/// Controls cross-site cookie behavior.
/// </summary>
public enum SameSiteMode
{
    /// <summary>
    /// Cookie is sent in all contexts; typically requires <c>Secure</c>.
    /// </summary>
    None,
    /// <summary>
    /// Cookie is withheld on cross-site subrequests but sent on top-level navigations.
    /// </summary>
    Lax,
    /// <summary>
    /// Cookie is sent only in a same-site context.
    /// </summary>
    Strict
}
