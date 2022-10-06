using System.ComponentModel.DataAnnotations;

namespace Engi.Substrate.Server.Types;

public class ImportUserKeyArguments
{
    [Required]
    public string EncryptedPkcs8Key { get; set; } = null!;
}