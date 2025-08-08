namespace Synack.Diagnostics;

public sealed class GuidTraceIdentifierProvider : ITraceIdentifierProvider
{
    public string GenerateTraceIdentifier()
    {
        return Guid.NewGuid().ToString("N");
    }
}
