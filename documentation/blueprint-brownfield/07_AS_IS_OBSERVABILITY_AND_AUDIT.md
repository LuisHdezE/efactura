# As-Is Observability and Audit

## Logging Técnico
- Serilog configurado para volcar en archivo y en Application Insights.

## Auditoría de Negocio
- **Auditoría**: No se observa un registro de auditoría durable para eventos de negocio. Sólo logging técnico. (`HYP-009: CONFIRMED`).

## Error Contract
- `ApiGlobalExceptionHandlerAttribute` maneja excepciones globalmente.
- **Inconsistencia**: `HandleNotFoundException` asigna el código `ErrorCode = "404"` pero el HTTP Status devuelto es `400 BadRequest`. (`HYP-006: CONFIRMED`).
