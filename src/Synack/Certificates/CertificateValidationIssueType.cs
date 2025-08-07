namespace Synack.Certificates;

/// <summary>
/// Specifies the type of issue encountered during X.509 certificate validation.
/// </summary>
public enum CertificateValidationIssueType
{
    /// <summary>
    /// The certificate does not contain a private key.
    /// </summary>
    MissingPrivateKey,

    /// <summary>
    /// The certificate's validity period has not yet started.
    /// </summary>
    NotYetValid,

    /// <summary>
    /// The certificate's validity period has expired.
    /// </summary>
    Expired,

    /// <summary>
    /// The certificate is missing the required Enhanced Key Usage extension for server authentication.
    /// </summary>
    MissingServerAuthUsage,

    /// <summary>
    /// The Enhanced Key Usage extension is present but malformed or unreadable.
    /// </summary>
    CorruptEnhancedKeyUsageExtension,

    /// <summary>
    /// A certificate validation issue occurred that does not fall into any of the predefined categories.
    /// </summary>
    Other
}
