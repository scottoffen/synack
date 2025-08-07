using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Synack.Certificates;

/// <summary>
/// Extension methods for validating X.509 certificates used in server authentication.
/// </summary>
public static class X509Certificate2Extensions
{
    internal static readonly string MessageMissingPrivateKey = "Certificate does not contain a private key.";
    internal static readonly string MessageNotYetValid = "Certificate is not valid until {0:u}.";
    internal static readonly string MessageExpired = "Certificate expired on {0:u}.";
    internal static readonly string MessageMissingServerAuth = "Certificate does not have 'Server Authentication' usage.";
    internal static readonly string MessageCorruptEku = "Certificate contains an Enhanced Key Usage extension that could not be decoded.";

    internal static readonly string ServerAuthOid = "1.3.6.1.5.5.7.3.1";

    /// <summary>
    /// Returns true if the certificate is valid for server authentication with no validation issues.
    /// </summary>
    public static bool IsValid(this X509Certificate2 cert)
    {
        return !cert.Validate().Any();
    }

    /// <summary>
    /// Returns true if the certificate is valid for server authentication; otherwise returns false and provides a list of validation issues.
    /// </summary>
    public static bool IsValid(this X509Certificate2 cert, out IEnumerable<CertificateValidationIssue> issues)
    {
        issues = cert.Validate();
        return !issues.Any();
    }

    /// <summary>
    /// Evaluates the certificate for common issues that would prevent it from being used for server authentication.
    /// </summary>
    public static IEnumerable<CertificateValidationIssue> Validate(this X509Certificate2 cert)
    {
        var now = DateTime.UtcNow;

        if (!cert.HasPrivateKey)
        {
            yield return new CertificateValidationIssue(
                CertificateValidationIssueType.MissingPrivateKey,
                MessageMissingPrivateKey);
        }

        if (now < cert.NotBefore)
        {
            yield return new CertificateValidationIssue(
                CertificateValidationIssueType.NotYetValid,
                string.Format(MessageNotYetValid, cert.NotBefore));
        }

        if (now > cert.NotAfter)
        {
            yield return new CertificateValidationIssue(
                CertificateValidationIssueType.Expired,
                string.Format(MessageExpired, cert.NotAfter));
        }

        var hasServerAuth = false;
        var ekuDecodeFailed = false;

        foreach (var ext in cert.Extensions.OfType<X509EnhancedKeyUsageExtension>())
        {
            try
            {
                if (ext.EnhancedKeyUsages.Cast<Oid>().Any(oid => oid.Value == ServerAuthOid))
                {
                    hasServerAuth = true;
                    break;
                }
            }
            catch (CryptographicException)
            {
                ekuDecodeFailed = true;
            }
        }

        if (!hasServerAuth)
        {
            yield return new CertificateValidationIssue(
                CertificateValidationIssueType.MissingServerAuthUsage,
                MessageMissingServerAuth);
        }

        if (ekuDecodeFailed)
        {
            yield return new CertificateValidationIssue(
                CertificateValidationIssueType.CorruptEnhancedKeyUsageExtension,
                MessageCorruptEku);
        }
    }
}
