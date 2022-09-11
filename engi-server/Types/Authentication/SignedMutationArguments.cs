using System.ComponentModel.DataAnnotations;
using System.Text;
using Engi.Substrate.Keys;

namespace Engi.Substrate.Server.Types.Authentication;

public class SignedMutationArguments : IValidatableObject
{
    [Required]
    public DateTime SignedOn { get; set; }

    [Required]
    public string Signature { get; set; } = null!;

    public bool IsValid(Address address, TimeSpan tolerance)
    {
        string expectedSignatureContent =
            $"{address}|{new DateTimeOffset(SignedOn).ToUniversalTime().ToUnixTimeMilliseconds()}";

        bool valid = address.Verify(
            Hex.GetBytes0X(Signature),
            Encoding.UTF8.GetBytes(expectedSignatureContent));

        return valid && SignedOn < DateTime.UtcNow.Add(tolerance);
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Signature.StartsWith("0x"))
        {
            yield return new ValidationResult("Must start with 0x",
                new[] { nameof(Signature) });
        }
    }
}