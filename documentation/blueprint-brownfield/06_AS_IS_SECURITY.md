# As-Is Security

## AuthN / AuthZ
- **JWT**: Autenticación Bearer configurada, pero con `RequireHttpsMetadata = false` (`HYP-008: CONFIRMED`).
- **CORS**: `AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()` (`HYP-007: CONFIRMED`).

## Secretos Versionados
- **Severidad**: P0 CRITICAL.
- **Evidencia** (`OBSERVED`): `appsettings.json` contiene secretos en claro (`HYP-001: CONFIRMED`).
  - `Azure Blob Account Key`: `<REDACTED>`
  - `JWT Signing Key`: `<REDACTED>`
  - `PostgreSQL password`: `<REDACTED>`
- **Recomendación Futura**: `rotate immediately before further exposure-sensitive work`.
