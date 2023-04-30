using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Engi.Substrate.Server;

public class JwtOptions
{
    [Required]
    public string Issuer { get; set; } = null!;

    [Required]
    public string Audience { get; set; } = null!;

    [Required]
    public string IssuerSigningCertificate { get; set; } = null!;

    public TimeSpan AccessTokenValidFor { get; set; }

    public TimeSpan RefreshTokenValidFor { get; set; }

    public RSA IssuerSigningKey => X509CertificatesHelper.CertificateFromBase64String(IssuerSigningCertificate).GetRSAPrivateKey()!;
}
