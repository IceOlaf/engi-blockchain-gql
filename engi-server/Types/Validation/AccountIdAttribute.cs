using System.ComponentModel.DataAnnotations;

namespace Engi.Substrate.Server.Types.Validation;

public class AccountIdAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        string s = (value as string)!;
        
        return Address.TryParse(s, out _);
    }

    public override string FormatErrorMessage(string name)
    {
        return "Account ID is invalid.";
    }
}