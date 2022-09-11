using System.Security.Cryptography.X509Certificates;

namespace Engi.Substrate.Server;

public class EngiOptions
{
    public string EncryptionCertificate { get; set; } = null!;

    public TimeSpan SignatureSkew { get; set; }

    public X509Certificate2 EncryptionCertificateAsX509 =>
        X509CertificatesHelper.CertificateFromBase64String(EncryptionCertificate);
}