using EFactura.Application.Common.Errors;

namespace WebApi.CrossCutting.Errors;

public sealed class V1ProblemDetailsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<V1ProblemDetailsMiddleware> _logger;

    public V1ProblemDetailsMiddleware(RequestDelegate next, ILogger<V1ProblemDetailsMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api/v1"))
        {
            await _next(context);
            return;
        }

        try
        {
            await _next(context);
        }
        catch (ApplicationProblemException exception) when (!context.Response.HasStarted)
        {
            var status = MapStatus(exception.Kind);
            _logger.LogWarning(
                "Application problem {ProblemCode} returned HTTP {StatusCode} for {RequestPath}",
                exception.Code,
                status,
                context.Request.Path);

            await V1ProblemDetailsResponse.WriteAsync(
                context,
                status,
                TitleFor(exception.Kind),
                exception.Message,
                exception.Code,
                exception.Errors,
                exception.ConflictType,
                exception.CurrentVersion,
                exception.RuleReferences,
                exception.RetryAfterSeconds);
        }
        catch (Exception exception) when (!context.Response.HasStarted)
        {
            _logger.LogError(exception, "Unhandled API v1 exception for {RequestPath}", context.Request.Path);
            await V1ProblemDetailsResponse.WriteAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "Internal server error",
                "An unexpected error occurred.");
        }
    }

    private static int MapStatus(ApplicationProblemKind kind) => kind switch
    {
        ApplicationProblemKind.BadRequest => StatusCodes.Status400BadRequest,
        ApplicationProblemKind.AuthenticationRequired => StatusCodes.Status401Unauthorized,
        ApplicationProblemKind.Forbidden => StatusCodes.Status403Forbidden,
        ApplicationProblemKind.NotFound => StatusCodes.Status404NotFound,
        ApplicationProblemKind.Conflict => StatusCodes.Status409Conflict,
        ApplicationProblemKind.Validation => StatusCodes.Status422UnprocessableEntity,
        ApplicationProblemKind.BusinessRule => StatusCodes.Status422UnprocessableEntity,
        ApplicationProblemKind.RequestTooLarge => StatusCodes.Status413PayloadTooLarge,
        ApplicationProblemKind.UnsupportedMediaType => StatusCodes.Status415UnsupportedMediaType,
        ApplicationProblemKind.RateLimited => StatusCodes.Status429TooManyRequests,
        ApplicationProblemKind.DependencyUnavailable => StatusCodes.Status503ServiceUnavailable,
        _ => StatusCodes.Status500InternalServerError
    };

    private static string TitleFor(ApplicationProblemKind kind) => kind switch
    {
        ApplicationProblemKind.BadRequest => "Bad request",
        ApplicationProblemKind.AuthenticationRequired => "Authentication required",
        ApplicationProblemKind.Forbidden => "Forbidden",
        ApplicationProblemKind.NotFound => "Not found",
        ApplicationProblemKind.Conflict => "Conflict",
        ApplicationProblemKind.Validation => "Validation failed",
        ApplicationProblemKind.BusinessRule => "Business rule rejected",
        ApplicationProblemKind.RequestTooLarge => "Request too large",
        ApplicationProblemKind.UnsupportedMediaType => "Unsupported media type",
        ApplicationProblemKind.RateLimited => "Rate limited",
        ApplicationProblemKind.DependencyUnavailable => "Dependency unavailable",
        _ => "Request failed"
    };
}
