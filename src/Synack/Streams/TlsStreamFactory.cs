using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Synack.Streams;

public class TlsStreamFactory
{
    private readonly ListenerOptions _options;

    public TlsStreamFactory(ListenerOptions options)
    {
        _options = options;
    }

    public async Task<SslStream> AuthenticateAsync(Stream rawStream, CancellationToken token)
    {
        var sslStream = new SslStream(
            rawStream,
            leaveInnerStreamOpen: false,
            userCertificateValidationCallback: (sender, certificate, chain, sslPolicyErrors) =>
            {
                // mTLS is not required
                if (!_options.RequireClientCertificate)
                    return true;

                // mTLS is required, but no certificate provided
                if (certificate is null)
                    return false;

                var cert2 = new X509Certificate2(certificate);

                // If custom validation is defined
                if (_options.ClientCertificateValidator is not null)
                    return _options.ClientCertificateValidator(cert2);

                // Default to system trust rules
                return sslPolicyErrors == SslPolicyErrors.None;
            });

        var authOptions = new SslServerAuthenticationOptions
        {
            ServerCertificate = _options.Certificate!,
            ClientCertificateRequired = _options.RequireClientCertificate,
            CertificateRevocationCheckMode = _options.CheckCertificateRevocation
                ? X509RevocationMode.Online
                : X509RevocationMode.NoCheck,
            EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
            ApplicationProtocols =
            [
                SslApplicationProtocol.Http2,
                SslApplicationProtocol.Http11
            ]
        };

        await sslStream.AuthenticateAsServerAsync(authOptions, token);
        return sslStream;
    }
}
