# As-Is Testing and Delivery

## Docker
- `Dockerfile` usa imágenes base `mcr.microsoft.com/dotnet/aspnet:6.0` y `sdk:6.0`, mientras que los proyectos están en `.NET 8.0` (`HYP-002: CONFIRMED`).

## CI/CD
- `.gitlab-ci.yml` contiene únicamente placeholders (echo "Building..."). (`HYP-010: CONFIRMED`).
- `azure-pipelines.yml` existe pero básico.

## Testing
- **Cobertura**: Proyecto `UnitTest` existe, pero fallaron en la primera compilación. La cobertura es baja respecto al dominio. (`HYP-012: CONFIRMED`).


## Build / Test Evidence
- **Restore result**: Success (Todos los proyectos est�n actualizados)
- **Build result**: Success with warnings
- **Warnings relevantes**: Obsolete telemetry configurations, nullable reference assignments.
- **Test result**: FAILED
- **Total passed**: 20
- **Total failed**: 1
- **Total skipped**: 0
- **Nombre exacto del test fallido**: ApplicationCore.Tests.Services.DepartmentServiceTests.GetById_Returns_Department_Successfully`n- **Causa observada**: OBSERVED - Assert.NotNull() Failure: Value is null en la l�nea 39 de DepartmentServiceTests.cs.
