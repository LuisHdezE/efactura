namespace EFactura.Application.Common.Context;

public sealed record CorrelationContext(string CorrelationId, string TraceId, string? CausationId = null)
{
    public static CorrelationContext Empty { get; } = new(string.Empty, string.Empty, null);
}

public interface ICorrelationContextAccessor
{
    CorrelationContext Current { get; }
}
