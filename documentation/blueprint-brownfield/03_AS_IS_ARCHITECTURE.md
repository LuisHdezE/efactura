# As-Is Architecture

## Arquitectura Declarada vs Observada
- **Declarada**: Clean Architecture.
- **Observada**: Clean Architecture Parcial.
- **Diferencias/Infracciones**:
  - `ApplicationCore` (la capa de dominio) tiene dependencias directas a `Microsoft.EntityFrameworkCore` y `Microsoft.AspNetCore.Http.Abstractions`. Esto rompe el principio de Clean Architecture de mantener el núcleo agnóstico a la infraestructura y la web.
  - Los servicios (ej. `CustomerService`) actúan en su mayoría como passthroughs hacia los repositorios sin agregar lógica de negocio sustancial (`HYP-005: CONFIRMED`).
