using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Synack.Tests.Extensions;

public class X509Certificate2ExtensionsTests
{
    [Fact]
    public void IsValid_ReturnsTrue_WhenCertificateIsValid()
    {
        var cert = TestCertificateFactory.Create();

        cert.IsValid().ShouldBeTrue();
    }

    [Fact]
    public void IsValid_ReturnsFalse_WhenCertificateIsMissingPrivateKey()
    {
        var cert = TestCertificateFactory.Create(includePrivateKey: false);

        cert.IsValid().ShouldBeFalse();
    }

    [Fact]
    public void IsValid_ReturnsFalse_WhenCertificateIsNotYetValid()
    {
        var future = DateTime.UtcNow.AddDays(1);
        var cert = TestCertificateFactory.Create(notBefore: future);

        cert.IsValid().ShouldBeFalse();
    }

    [Fact]
    public void IsValid_ReturnsFalse_WhenCertificateIsExpired()
    {
        var notBefore = DateTime.UtcNow.AddDays(-10);
        var notAfter = DateTime.UtcNow.AddDays(-1); // Cert expired yesterday
        var cert = TestCertificateFactory.Create(notBefore: notBefore, notAfter: notAfter);

        cert.IsValid().ShouldBeFalse();
    }

    [Fact]
    public void IsValid_ReturnsFalse_WhenCertificateMissingServerAuthUsage()
    {
        var cert = TestCertificateFactory.Create(includeServerAuth: false);

        cert.IsValid().ShouldBeFalse();
    }

    [Fact]
    public void Validate_ReturnsMissingPrivateKey_WhenPrivateKeyIsMissing()
    {
        var cert = TestCertificateFactory.Create(includePrivateKey: false);

        var issues = cert.Validate().ToList();

        issues.ShouldContain(x => x.Type == CertificateValidationIssueType.MissingPrivateKey);
    }

    [Fact]
    public void Validate_ReturnsNotYetValid_WhenCurrentDateIsBeforeNotBefore()
    {
        var notBefore = DateTime.UtcNow.AddDays(1);               // Much safer margin
        var notAfter = notBefore.AddDays(1);                      // Ensure valid cert range
        var cert = TestCertificateFactory.Create(notBefore: notBefore, notAfter: notAfter);

        var issues = cert.Validate().ToList();

        issues.ShouldContain(x => x.Type == CertificateValidationIssueType.NotYetValid);
    }

    [Fact]
    public void Validate_ReturnsExpired_WhenCurrentDateIsAfterNotAfter()
    {
        var notAfter = DateTime.UtcNow.AddHours(-2);
        var cert = TestCertificateFactory.Create(notAfter: notAfter);

        var issues = cert.Validate().ToList();

        issues.ShouldContain(x => x.Type == CertificateValidationIssueType.Expired);
    }

    [Fact]
    public void Validate_ReturnsMissingServerAuthUsage_WhenServerAuthNotPresent()
    {
        var cert = TestCertificateFactory.Create(includeServerAuth: false);

        var issues = cert.Validate().ToList();

        issues.ShouldContain(x => x.Type == CertificateValidationIssueType.MissingServerAuthUsage);
    }

    [Fact]
    public void IsValid_ReturnsFalse_WhenMultipleValidationIssuesExist()
    {
        var notBefore = DateTime.UtcNow.AddDays(2);  // Not yet valid
        var notAfter = notBefore.AddDays(1);         // Still valid range

        var cert = TestCertificateFactory.Create(
            includePrivateKey: false,
            includeServerAuth: false,
            notBefore: notBefore,
            notAfter: notAfter);

        var result = cert.IsValid(out var issues);

        result.ShouldBeFalse();

        issues.Select(x => x.Type).ShouldBeSubsetOf(new[]
        {
            CertificateValidationIssueType.MissingPrivateKey,
            CertificateValidationIssueType.MissingServerAuthUsage,
            CertificateValidationIssueType.NotYetValid
        });
    }

    [Fact]
    public void IsValid_ReturnsTrue_AndNoIssues_WhenCertificateIsValid()
    {
        var cert = TestCertificateFactory.Create();

        var result = cert.IsValid(out var issues);

        result.ShouldBeTrue();
        issues.ShouldBeEmpty();
    }

    [Fact]
    public void Validate_IssuesContainNonEmptyMessages()
    {
        var notBefore = DateTime.UtcNow.AddDays(1);
        var cert = TestCertificateFactory.Create(includePrivateKey: false, includeServerAuth: false, notBefore: notBefore, notAfter: notBefore.AddDays(1));

        var issues = cert.Validate().ToList();

        issues.ShouldAllBe(x => !string.IsNullOrWhiteSpace(x.Message));
    }

    [Fact]
    public void Validate_ReturnsMissingServerAuthUsage_WhenEnhancedKeyUsageExtensionIsEmpty()
    {
        var ecdsa = ECDsa.Create();
        var req = new CertificateRequest("CN=TestCert", ecdsa, HashAlgorithmName.SHA256);

        var emptyEku = new OidCollection(); // No usages
        req.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(emptyEku, false));

        var cert = req.CreateSelfSigned(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1));

        var issues = cert.Validate().ToList();

        issues.ShouldContain(x => x.Type == CertificateValidationIssueType.MissingServerAuthUsage);
    }

    [Fact]
    public void Validate_DoesNotThrow_WhenNonParseableEnhancedKeyUsageExtensionExists()
    {
        var ecdsa = ECDsa.Create();
        var req = new CertificateRequest("CN=TestCert", ecdsa, HashAlgorithmName.SHA256);

        // Add a bogus extension with the Server Auth OID, but with invalid raw data
        var corrupted = new X509Extension("2.5.29.37", new byte[] { 0x30, 0xFF, 0x00 }, critical: false);
        req.CertificateExtensions.Add(corrupted);

        var cert = req.CreateSelfSigned(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1));

        var ex = Record.Exception(() => cert.Validate().ToList());

        ex.ShouldBeNull(); // The method should ignore invalid extensions gracefully
    }

    [Fact]
    public void Validate_ReturnsCorruptEkuIssue_WhenEkuExtensionCannotBeDecoded()
    {
        var ecdsa = ECDsa.Create();
        var req = new CertificateRequest("CN=TestCert", ecdsa, HashAlgorithmName.SHA256);

        var corrupted = new X509Extension("2.5.29.37", new byte[] { 0x30, 0xFF, 0x00 }, critical: false);
        req.CertificateExtensions.Add(corrupted);

        var cert = req.CreateSelfSigned(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1));

        var issues = cert.Validate().ToList();

        issues.ShouldContain(x => x.Type == CertificateValidationIssueType.CorruptEnhancedKeyUsageExtension);
    }

    [Fact]
    public void Validate_ReturnsCorruptEkuAndMissingServerAuth_WhenEkuCannotBeDecoded()
    {
        var ecdsa = ECDsa.Create();
        var req = new CertificateRequest("CN=TestCert", ecdsa, HashAlgorithmName.SHA256);

        // Malformed EKU extension
        var corrupted = new X509Extension("2.5.29.37", new byte[] { 0x30, 0xFF, 0x00 }, critical: false);
        req.CertificateExtensions.Add(corrupted);

        var cert = req.CreateSelfSigned(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1));

        var issues = cert.Validate().ToList();

        issues.ShouldContain(x => x.Type == CertificateValidationIssueType.CorruptEnhancedKeyUsageExtension);
        issues.ShouldContain(x => x.Type == CertificateValidationIssueType.MissingServerAuthUsage);
    }


    [Fact]
    public void Validate_ReturnsMissingServerAuth_WhenOnlyUnknownEkusPresent()
    {
        var ecdsa = ECDsa.Create();
        var req = new CertificateRequest("CN=TestCert", ecdsa, HashAlgorithmName.SHA256);

        var eku = new OidCollection
        {
            new Oid("1.3.6.1.5.5.7.3.2") // Client Authentication (not Server Auth)
        };

        req.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(eku, false));

        var cert = req.CreateSelfSigned(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1));

        var issues = cert.Validate().ToList();

        issues.ShouldContain(x => x.Type == CertificateValidationIssueType.MissingServerAuthUsage);
    }
}
