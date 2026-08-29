using EFactura.Application.Common.Context;
using Serilog.Context;

namespace WebApi.CrossCutting.Context;

public sealed class ActorLogContextMiddleware
{
    private readonly RequestDelegate _next;

    public ActorLogContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IActorContextAccessor actorContextAccessor)
    {
        var actor = actorContextAccessor.Current;
        if (!actor.IsAuthenticated)
        {
            await _next(context);
            return;
        }

        using var actorProperty = LogContext.PushProperty("ActorId", actor.ActorId ?? "unknown");
        using var usernameProperty = LogContext.PushProperty("Username", actor.ActorId ?? "unknown");
        using var deviceProperty = LogContext.PushProperty("DeviceId", actor.DeviceId ?? string.Empty);

        await _next(context);
    }
}
