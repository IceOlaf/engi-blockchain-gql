using Engi.Substrate.Server.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace Engi.Substrate.Server;

public class NonSudoAuthorizationRequirement : AuthorizationHandler<NonSudoAuthorizationRequirement>, IAuthorizationRequirement
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, NonSudoAuthorizationRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated == true && !context.User.IsInRole(Roles.Sudo))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}