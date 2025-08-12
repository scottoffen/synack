using System.Diagnostics.CodeAnalysis;

namespace Synack.Exceptions;

[ExcludeFromCodeCoverage]
public sealed class RequestLimitExceededException : Exception
{
    public string LimitName { get; }
    public int ConfiguredLimit { get; }
    public int ObservedValue { get; }

    public RequestLimitExceededException(string limitName, int configuredLimit, int observedValue)
        : base($"{limitName} exceeded: {observedValue} > {configuredLimit}")
    {
        LimitName = limitName;
        ConfiguredLimit = configuredLimit;
        ObservedValue = observedValue;
    }
}
