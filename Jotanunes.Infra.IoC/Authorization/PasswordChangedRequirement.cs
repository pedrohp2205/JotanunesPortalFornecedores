using Jotanunes.Infra.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Jotanunes.Infra.IoC.Authorization;

public class PasswordChangedRequirement : AuthorizationHandler<PasswordChangedRequirement>, IAuthorizationRequirement
{
    public const string Message = "Troque a senha provisória para continuar.";

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PasswordChangedRequirement requirement)
    {
        if (context.User.FindFirst(SupplierUserClaims.MustChangePassword)?.Value != "true")
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
