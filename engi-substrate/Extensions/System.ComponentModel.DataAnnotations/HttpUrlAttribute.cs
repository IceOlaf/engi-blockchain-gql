namespace System.ComponentModel.DataAnnotations;

public sealed class HttpUrlAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is string url && Uri.TryCreate(url, UriKind.Absolute, out var validatedUri))
        {
            return validatedUri.Scheme == Uri.UriSchemeHttp || validatedUri.Scheme == Uri.UriSchemeHttps;
        }

        return false;
    }
}
