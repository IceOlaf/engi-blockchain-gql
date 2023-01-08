using Engi.Substrate.Identity;

namespace Engi.Substrate.Server.Email;

public class ConfirmEmailDispatchCommand : EmailDispatchCommand
{
    private ConfirmEmailDispatchCommand() { }

    public ConfirmEmailDispatchCommand(User user, ApplicationOptions applicationOptions)
    {
        var emailConfirmationToken = user.Tokens
            .OfType<EmailConfirmationToken>()
            .SingleOrDefault();

        if (emailConfirmationToken == null)
        {
            throw new ArgumentOutOfRangeException(nameof(user),
                "User does not have a confirmation token.");
        }

        UserId = user.Id;
        TemplateName = "ConfirmEmail";
        Data = new()
        {
            ["Url"] = $"{applicationOptions.Url}/signup/confirm/{user.Address}?token={emailConfirmationToken.Value}"
        };
    }
}
