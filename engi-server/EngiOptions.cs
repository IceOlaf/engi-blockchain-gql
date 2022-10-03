using System.Security.Cryptography.X509Certificates;

namespace Engi.Substrate.Server;

public class EngiOptions
{
    /// <summary>
    /// The encryption certificate to use to store keys.
    /// </summary>
    public string EncryptionCertificate { get; set; } = null!;

    /// <summary>
    /// The allowed skew for signature validation.
    /// </summary>
    public TimeSpan SignatureSkew { get; set; }

    /// <summary>
    ///  Used to disable the chain observer during testing.
    /// </summary>
    public bool DisableChainObserver { get; set; }

    /// <summary>
    /// The API key for sudo calls.
    /// </summary>
    public string SudoApiKey { get; set; } = null!;

    public X509Certificate2 EncryptionCertificateAsX509 =>
        X509CertificatesHelper.CertificateFromBase64String(EncryptionCertificate);
}