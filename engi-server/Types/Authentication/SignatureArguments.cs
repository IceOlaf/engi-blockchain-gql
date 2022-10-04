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

        string wrappedSignatureContent = $"<Bytes>{expectedSignatureContent}</Bytes>";

        byte[] valueBytes = Hex.GetBytes0X(Value);

        // first try to verify wrapped as most expected case, then raw

        bool valid = address.Verify(valueBytes,
            Encoding.UTF8.GetBytes(wrappedSignatureContent));

        if (!valid)
        {
            valid = address.Verify(valueBytes,
                Encoding.UTF8.GetBytes(expectedSignatureContent));
        }

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