using System.Diagnostics.CodeAnalysis;

namespace Synack.Exceptions;

[ExcludeFromCodeCoverage]
public abstract class SynackException : Exception
{
    protected SynackException(string message)
        : base(message)
    {
    }

    protected SynackException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
