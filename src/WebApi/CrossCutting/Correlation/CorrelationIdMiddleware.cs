using System.Diagnostics;
using Serilog.Context;

namespace WebApi.CrossCutting.Correlation;

public sealed class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var supplied = context.Request.Headers[CorrelationContextKeys.HeaderName].ToString();
        var correlationId = IsSafeCorrelationId(supplied)
            ? supplied
            : Guid.NewGuid().ToString("N");

        var traceId = Activity.Current?.TraceId.ToString();
        if (string.IsNullOrWhiteSpace(traceId))
        {
            traceId = context.TraceIdentifier;
        }

        context.Items[CorrelationContextKeys.CorrelationIdItem] = correlationId;
        context.Items[CorrelationContextKeys.TraceIdItem] = traceId;
        context.Response.Headers[CorrelationContextKeys.HeaderName] = correlationId;

        using var correlationProperty = LogContext.PushProperty("CorrelationId", correlationId);
        using var traceProperty = LogContext.PushProperty("TraceId", traceId);
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["TraceId"] = traceId
        });

        await _next(context);
    }

    private static bool IsSafeCorrelationId(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 128)
        {
            return false;
        }

        foreach (var character in value)
        {
            if (!(char.IsLetterOrDigit(character) || character is '-' or '_' or '.' or ':'))
            {
                return false;
            }
        }

        return true;
    }
}
