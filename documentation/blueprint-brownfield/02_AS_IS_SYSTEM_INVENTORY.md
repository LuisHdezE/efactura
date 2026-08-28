# As-Is System Inventory

## Runtime y lenguaje
- **Versión real de .NET**: `.NET 8.0` (`<TargetFramework>net8.0</TargetFramework>` en los proyectos).
- **Lenguaje**: C# (`<LangVersion>preview</LangVersion>`).
- **Nullable Context**: Mixto (`disable` en ApplicationCore e Infrastructure, `enable` en WebApi y Shared).

## Dependencies
- Dapper 2.1.35
- EntityFrameworkCore 8.0.10
- Serilog 8.0.1
- AutoMapper 12.0.1
- Swagger 6.5.0
