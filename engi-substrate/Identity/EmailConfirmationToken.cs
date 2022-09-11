namespace Engi.Substrate.Identity;

public class EmailConfirmationToken : UserToken
{
    public EmailConfirmationToken()
    {
        Value = Guid.NewGuid().ToString("n");
    }
}