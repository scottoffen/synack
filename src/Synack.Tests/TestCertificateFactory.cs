using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Synack.Tests;

public static class TestCertificateFactory
{
    public static X509Certificate2 Create(
        string subjectName,
        bool includePrivateKey = true,
        bool includeServerAuth = true,
        DateTime? notBefore = null,
        DateTime? notAfter = null
        )
    {
        var ecdsa = ECDsa.Create(); // you can also use RSA.Create()
        var request = new CertificateRequest($"CN={subjectName}", ecdsa, HashAlgorithmName.SHA256);

        if (includeServerAuth)
        {
            request.CertificateExtensions.Add(
                new X509EnhancedKeyUsageExtension(
                    new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, // ServerAuth
                    false)); // protocol is using true
        }

        var cert = request.CreateSelfSigned(
            notBefore ?? DateTime.UtcNow.AddDays(-1),
            notAfter ?? DateTime.UtcNow.AddDays(1));

        if (!includePrivateKey)
        {
            // Strip private key by exporting/importing the public part only
            return new X509Certificate2(cert.Export(X509ContentType.Cert));
        }

        return new X509Certificate2(cert.Export(X509ContentType.Pfx));
    }
}
