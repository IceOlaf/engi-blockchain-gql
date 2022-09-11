using System.Security.Cryptography.X509Certificates;

namespace Engi.Substrate.Server;

class X509CertificatesHelper
{
    public static X509Certificate2 CertificateFromBase64String(string certificate)
    {
        return !string.IsNullOrEmpty(certificate)
            ? new X509Certificate2(Convert.FromBase64String(certificate))
            : throw new ArgumentNullException(nameof(certificate));
    }
}