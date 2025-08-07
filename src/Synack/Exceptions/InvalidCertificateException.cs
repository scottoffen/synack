using System.Diagnostics.CodeAnalysis;
using System.Text;
using Synack.Certificates;

namespace Synack.Exceptions;

[ExcludeFromCodeCoverage]
public sealed class InvalidCertificateException : SynackException
{
    public IReadOnlyList<CertificateValidationIssue> Issues { get; }

    public InvalidCertificateException(IEnumerable<CertificateValidationIssue> issues)
        : base("The provided certificate failed validation.")
    {
        Issues = [.. issues];
    }

    public override string ToString()
    {
        var sb = new StringBuilder(base.ToString());
        foreach (var issue in Issues)
        {
            sb.AppendLine($" - {issue.Type}: {issue.Message}");
        }
        return sb.ToString();
    }
}
