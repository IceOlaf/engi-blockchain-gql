using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Engi.Substrate.Server.Controllers;

[Route("api/engi"), ApiController]
public class EngiController : ControllerBase
{
    private readonly EngiOptions options;

    public EngiController(IOptions<EngiOptions> options)
    {
        this.options = options.Value;
    }

    [HttpGet("public-key"), AllowAnonymous]
    public IActionResult GetPublicKey()
    {
        var publicKey = options.EncryptionCertificateAsX509
            .GetRSAPublicKey()!
            .ExportSubjectPublicKeyInfo();

        return File(new MemoryStream(publicKey, false), "application/octet-stream");
    }
}