namespace WebApi.CrossCutting.Correlation;

public static class CorrelationContextKeys
{
    public const string HeaderName = "X-Correlation-Id";
    public const string CorrelationIdItem = "efactura.correlation_id";
    public const string TraceIdItem = "efactura.trace_id";
}
