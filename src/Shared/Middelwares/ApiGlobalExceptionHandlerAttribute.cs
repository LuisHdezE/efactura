using ApplicationCore.Exceptions;
using ApplicationCore.ValueObjects.Result;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Serilog;

namespace Shared.Middelwares
{
    public class ApiGlobalExceptionHandlerAttribute : ExceptionFilterAttribute
    {
        private readonly IDictionary<Type, Action<ExceptionContext>> _exceptionHandlers;

        public ApiGlobalExceptionHandlerAttribute()
        {
            // Register known exception types and handlers.
            _exceptionHandlers = new Dictionary<Type, Action<ExceptionContext>>
            {
                { typeof(ProviderNotFoundException), HandleNotFoundException },
                { typeof(DBOperationException), HandleDBOperationException },
                { typeof(GenericException), HandleGenericException },
            };
        }

        /// <summary>
        /// Legacy exception contract. API v1 deliberately bypasses this filter and is handled
        /// by the RFC 9457 middleware in WebApi.
        /// </summary>
        /// <param name="context"></param>
        public override void OnException(ExceptionContext context)
        {
            if (context.HttpContext.Request.Path.StartsWithSegments("/api/v1"))
            {
                base.OnException(context);
                return;
            }

            HandleException(context);

            base.OnException(context);
        }

        private void HandleException(ExceptionContext context)
        {
            Log.Fatal($"Exception Message: {context?.Exception?.Message}, " +
                $"Exception StackTrace: {context?.Exception?.StackTrace}, " +
                $"Exception Source: {context?.Exception?.Source}, " +
                $"Exception InnerException: {context?.Exception?.InnerException}");

            Type? type = context?.Exception?.GetType();
            if (type != null && _exceptionHandlers.ContainsKey(type))
            {
                _exceptionHandlers[type].Invoke(context);
                return;
            }
            else
            {
                HandleGenericException(context);
                return;
            }

            if (!context.ModelState.IsValid)
            {
                HandleInvalidModelStateException(context);
                return;
            }
        }

        private void HandleGenericException(ExceptionContext context)
        {
            var envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var stackTrace = !string.IsNullOrEmpty(envName) && envName.Equals("Development") ? $" Exception StackTrace: {context?.Exception?.StackTrace}" : string.Empty;

            ResultObject details = new ResultObject
            {
                Message = context?.Exception?.Message,
                ErrorCode = StatusCodes.Status500InternalServerError.ToString(),
                Detail = context?.Exception?.Source ?? String.Empty,
                Data = stackTrace,
            };

            context.Result = new ObjectResult(details)
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };

            context.ExceptionHandled = true;
        }

        private void HandleInvalidModelStateException(ExceptionContext context)
        {
            ValidationProblemDetails details = new ValidationProblemDetails(context.ModelState)
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
            };

            context.Result = new BadRequestObjectResult(details);

            context.ExceptionHandled = true;
        }

        private void HandleNotFoundException(ExceptionContext context)
        {
            var envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var stackTrace = !string.IsNullOrEmpty(envName) && envName.Equals("Development") ? $" Exception StackTrace: {context?.Exception?.StackTrace}" : string.Empty;
            ResultObject details = new ResultObject
            {
                Message = context?.Exception?.Message,
                ErrorCode = StatusCodes.Status404NotFound.ToString(),
                Detail = context?.Exception?.Source ?? String.Empty,
                Data = stackTrace,
            };

            context.Result = new ObjectResult(details)
            {
                StatusCode = StatusCodes.Status400BadRequest
            };

            context.ExceptionHandled = true;
        }

        private void HandleDBOperationException(ExceptionContext context)
        {
            var envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var stackTrace = !string.IsNullOrEmpty(envName) && envName.Equals("Development") ? $" Exception StackTrace: {context?.Exception?.StackTrace}" : string.Empty;
            ResultObject details = new ResultObject
            {
                Message = context?.Exception?.Message,
                ErrorCode = StatusCodes.Status500InternalServerError.ToString(),
                Detail = context?.Exception?.Source ?? String.Empty,
                Data = stackTrace,
            };

            context.Result = new ObjectResult(details)
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };

            context.ExceptionHandled = true;
        }
    }
}
