using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace WebApi.CrossCutting.Authorization;

public sealed class V1AuthorizationMiddlewareResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _fallback = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (!context.Request.Path.StartsWithSegments("/api/v1"))
        {
            await _fallback.HandleAsync(next, context, policy, authorizeResult);
            return;
        }

        if (authorizeResult.Challenged)
        {
            await Errors.V1ProblemDetailsResponse.WriteAsync(
                context,
                StatusCodes.Status401Unauthorized,
                "Authentication required",
                "A valid bearer token is required for this operation.",
                "authentication_required");
            return;
        }

        if (authorizeResult.Forbidden)
        {
            await Errors.V1ProblemDetailsResponse.WriteAsync(
                context,
                StatusCodes.Status403Forbidden,
                "Forbidden",
                "The authenticated actor does not have permission for this operation.",
                "forbidden");
            return;
        }

        await _fallback.HandleAsync(next, context, policy, authorizeResult);
    }
}
