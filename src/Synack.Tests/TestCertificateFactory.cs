using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Synack.Tests;

public static class TestCertificateFactory
{
    public static X509Certificate2 CreateSelfSignedServerCertificate(string subjectName)
    {
        var ecdsa = ECDsa.Create(); // you can also use RSA.Create()
        var request = new CertificateRequest($"CN={subjectName}", ecdsa, HashAlgorithmName.SHA256);

        request.CertificateExtensions.Add(
            new X509EnhancedKeyUsageExtension(
                new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, // Server Authentication
                critical: true));

        var cert = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(7));
        return new X509Certificate2(cert.Export(X509ContentType.Pfx));

    }
}

