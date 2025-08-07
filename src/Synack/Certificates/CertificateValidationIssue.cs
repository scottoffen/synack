namespace Synack.Certificates;

/// <summary>
/// Represents an issue identified during X.509 certificate validation.
/// </summary>
/// <param name="Type">The type/category of the certificate validation issue.</param>
/// <param name="Message">A descriptive message detailing the validation issue.</param>
public sealed record CertificateValidationIssue(
    CertificateValidationIssueType Type,
    string Message
);
