using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using EFactura.Application.Common.Errors;
using WebApi.CrossCutting.Correlation;

namespace WebApi.CrossCutting.Errors;

public static class V1ProblemDetailsResponse
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static async Task WriteAsync(
        HttpContext context,
        int status,
        string title,
        string detail,
        string? code = null,
        IReadOnlyList<ApplicationFieldError>? errors = null,
        string? conflictType = null,
        string? currentVersion = null,
        IReadOnlyList<ApplicationRuleReference>? ruleReferences = null,
        int? retryAfterSeconds = null)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";

        if (retryAfterSeconds is > 0)
        {
            context.Response.Headers.RetryAfter = retryAfterSeconds.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        var correlationId = context.Items[CorrelationContextKeys.CorrelationIdItem]?.ToString();
        var traceId = context.Items[CorrelationContextKeys.TraceIdItem]?.ToString()
            ?? Activity.Current?.TraceId.ToString()
            ?? context.TraceIdentifier;

        var document = new Dictionary<string, object?>
        {
            ["type"] = code is null ? "about:blank" : $"urn:efactura:problem:{code}",
            ["title"] = title,
            ["status"] = status,
            ["detail"] = detail,
            ["instance"] = context.Request.Path.Value,
            ["code"] = code,
            ["traceId"] = traceId,
            ["correlationId"] = correlationId,
            ["errors"] = errors is { Count: > 0 } ? errors : null,
            ["conflictType"] = conflictType,
            ["currentVersion"] = currentVersion,
            ["ruleReferences"] = ruleReferences is { Count: > 0 } ? ruleReferences : null,
            ["retryAfterSeconds"] = retryAfterSeconds
        };

        await JsonSerializer.SerializeAsync(context.Response.Body, document, JsonOptions, context.RequestAborted);
    }
}
