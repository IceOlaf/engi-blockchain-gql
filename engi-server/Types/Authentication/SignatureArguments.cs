using System.ComponentModel.DataAnnotations;
using System.Text;
using Engi.Substrate.Keys;

namespace Engi.Substrate.Server.Types.Authentication;

public sealed class SignatureArguments : IValidatableObject
{
    [Required]
    public DateTime SignedOn { get; set; }

    [Required]
    public string Value { get; set; } = null!;

    public bool IsValid(Address address, TimeSpan tolerance)
    {
        string expectedSignatureContent =
            $"{address}|{new DateTimeOffset(SignedOn).ToUniversalTime().ToUnixTimeMilliseconds()}";

        bool valid = address.Verify(
            Hex.GetBytes0X(Value),
            Encoding.UTF8.GetBytes(expectedSignatureContent));

        return valid && SignedOn < DateTime.UtcNow.Add(tolerance);
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Value.StartsWith("0x"))
        {
            yield return new ValidationResult("Must start with 0x",
                new[] { nameof(Value) });
        }
    }
}