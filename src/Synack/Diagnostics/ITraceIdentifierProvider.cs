namespace Synack.Diagnostics;

public interface ITraceIdentifierProvider
{
    /// <summary>
    /// Generates a unique identifier to associate with an incoming connection or request.
    /// </summary>
    /// <returns>A trace identifier string.</returns>
    string GenerateTraceIdentifier();
}

