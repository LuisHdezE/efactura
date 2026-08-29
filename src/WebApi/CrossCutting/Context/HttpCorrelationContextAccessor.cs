using EFactura.Application.Common.Context;
using WebApi.CrossCutting.Correlation;

namespace WebApi.CrossCutting.Context;

public sealed class HttpCorrelationContextAccessor : ICorrelationContextAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpCorrelationContextAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public CorrelationContext Current
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            if (context is null)
            {
                return CorrelationContext.Empty;
            }

            var correlationId = context.Items[CorrelationContextKeys.CorrelationIdItem]?.ToString() ?? string.Empty;
            var traceId = context.Items[CorrelationContextKeys.TraceIdItem]?.ToString() ?? context.TraceIdentifier;
            return new CorrelationContext(correlationId, traceId);
        }
    }
}
