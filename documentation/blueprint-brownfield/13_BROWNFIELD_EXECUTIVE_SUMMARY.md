# Brownfield Executive Summary

La inspecciÃ³n Brownfield de `eFactura` revela un proyecto con fundamentos sÃ³lidos en .NET 8, pero con mÃºltiples desajustes tÃ©cnicos (gaps).

El hallazgo mÃ¡s crÃ­tico son secretos versionados en `appsettings.json` (P0). Adicionalmente, existen inconsistencias en la arquitectura (Core dependiente de EF Core), seguridad (CORS abierto, JWT sin HTTPS), y en el ciclo de vida (Dockerfile desfasado y tests insuficientes/fallidos).

Las 12 hipÃ³tesis planteadas preliminarmente han sido **CONFIRMADAS**.

No se recomienda avanzar sin remediaciones secuenciales focalizadas, preservando el stack actual.


## Hypothesis Validation Matrix

| Hypothesis | Status | Evidence | Notes |
|---|---|---|---|
| HYP-001 | CONFIRMED | appsettings.json expone Azure Blob Key, JWT Key, y contraseñas DB | P0 Security Risk |
| HYP-002 | CONFIRMED | Dockerfile usa sdk:6.0 y aspnet:6.0 mientras csproj usa net8.0 | |
| HYP-003 | CONFIRMED | ApplicationCore.csproj referencia a Microsoft.EntityFrameworkCore | Violación arquitectónica |
| HYP-004 | CONFIRMED | Modelos Customer y Customers coexisten en ApplicationCore.Entities | Duplicidad de dominio |
| HYP-005 | CONFIRMED | CustomerService actúa como wrapper del repositorio sin lógica extra | |
| HYP-006 | CONFIRMED | HandleNotFoundException devuelve StatusCode 400 en lugar de 404 | Contrato de error inconsistente |
| HYP-007 | CONFIRMED | Program.cs contiene AllowAnyOrigin y AllowAnyMethod | Riesgo CSRF |
| HYP-008 | CONFIRMED | JwtBearerOptions.RequireHttpsMetadata es false | Riesgo MITM |
| HYP-009 | CONFIRMED | Uso de Serilog existente, sin Audit log de negocio detectable | Faltan repositorios de auditoría |
| HYP-010 | CONFIRMED | .gitlab-ci.yml solo ejecuta \echo\ | Pipeline simulado |
| HYP-011 | CONFIRMED | README.md menciona .Net 6.0.4 pero los csproj indican net8.0 | |
| HYP-012 | CONFIRMED | Sólo 21 tests totales, insuficiente para el dominio de 24 entidades | Cobertura baja |
