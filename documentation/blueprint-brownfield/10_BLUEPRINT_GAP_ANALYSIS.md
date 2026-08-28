# Blueprint Gap Analysis

| ID | Área | Estado | Severidad | Evidencia | Riesgo | Recomendación futura |
| -- | ---- | ------ | --------- | --------- | ------ | -------------------- |
| GAP-001 | Security | GAP | P0 | Secretos expuestos en `appsettings.json` | Exposición de BD y Cloud | Rotar inmediatamente y eliminar del control de versiones. |
| GAP-002 | Security | GAP | P1 | CORS excesivamente permisivo en `Program.cs` | Ataques CSRF | Restringir orígenes. |
| GAP-003 | Security | GAP | P1 | `RequireHttpsMetadata = false` en JWT | MITM | Requerir HTTPS. |
| GAP-004 | Infrastructure | GAP | P2 | Dockerfile utiliza SDK 6.0, csproj utiliza 8.0 | Falla en despliegue | Actualizar Dockerfile a 8.0. |
| GAP-005 | Architecture | GAP | P2 | `ApplicationCore` depende de EF Core | Acoplamiento de infraestructura | Remover dependencias tecnológicas del dominio. |
| GAP-006 | Error Handling | GAP | P2 | Código de error 404 devuelve HTTP 400 | Contrato confuso | Alinear HTTP Status con ErrorCode lógico. |
| GAP-007 | Observability | GAP | P1 | Falta auditoría de negocio | Incapacidad de rastrear acciones | Implementar Audit log durable. |
| GAP-008 | Data Model | GAP | P2 | Entidades duplicadas (Customer/Customers) | Ambigüedad en mantenimiento | Consolidar modelos. |
| GAP-009 | CI/CD | GAP | P2 | Pipeline en GitLab es un placeholder | Falsa sensación de seguridad | Implementar CI/CD real. |
| GAP-010 | Documentation | GAP | P3 | README desactualizado | Confusión para desarrolladores | Actualizar documentación. |
