using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;

namespace Jotanunes.Infra.IoC.Authorization;

public class PasswordChangeResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _default = new();

    public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
    {
        var mustChangePassword = authorizeResult.Forbidden
            && authorizeResult.AuthorizationFailure?.FailedRequirements.OfType<PasswordChangedRequirement>().Any() == true;

        if (!mustChangePassword)
        {
            await _default.HandleAsync(next, context, policy, authorizeResult);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            Message = PasswordChangedRequirement.Message,
            MustChangePassword = true
        }));
    }
}
