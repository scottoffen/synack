using System.Diagnostics.CodeAnalysis;

namespace Synack.Diagnostics;

[ExcludeFromCodeCoverage]
public sealed class GuidTraceIdentifierProvider : ITraceIdentifierProvider
{
    public string GenerateTraceIdentifier()
    {
        return Guid.NewGuid().ToString("N");
    }
}
