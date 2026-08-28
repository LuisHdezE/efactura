# Documentation Drift

## Evidencia
- El `README.md` indica que se utiliza `.Net 6.0.4 LTS`, pero la implementación real utiliza `<TargetFramework>net8.0</TargetFramework>` (`HYP-011: CONFIRMED`).
- Múltiples inconsistencias entre lo documentado y lo implementado (ej. Docker vs csproj, Serilog version, etc).
