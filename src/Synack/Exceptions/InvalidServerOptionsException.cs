using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Synack.Exceptions;

[ExcludeFromCodeCoverage]
public sealed class InvalidServerOptionsException : SynackException
{
    public IReadOnlyList<string> Issues { get; }

    public InvalidServerOptionsException(IEnumerable<string> issues)
        : base("The server options are invalid.")
    {
        Issues = [.. issues];
    }

    public override string ToString()
    {
        var sb = new StringBuilder(base.ToString());
        foreach (var issue in Issues)
            sb.AppendLine($" - {issue}");
        return sb.ToString();
    }
}
