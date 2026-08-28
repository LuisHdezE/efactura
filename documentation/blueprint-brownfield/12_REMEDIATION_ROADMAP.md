# Remediation Roadmap

## Secuencia Propuesta (NO EJECUTAR TODAVÍA)
1. **P0 Security / Secrets**: Rotación de secretos, limpieza de `appsettings.json`, y configuración de Secure Vault/Environment Variables.
2. **Runtime/Container Alignment**: Actualizar Dockerfile a `.NET 8`.
3. **CORS & JWT Fortification**: Ajustar políticas en `Program.cs`.
4. **Data Model Reconciliation**: Resolver la duplicación de modelos (`Customer` vs `Customers`).
5. **Architecture Implementation Conformance**: Remover dependencias de EF Core y Web en `ApplicationCore`.
6. **Error Contract**: Ajustar manejadores de excepciones para coherencia HTTP.
7. **Business Audit**: Implementar repositorio y logs durables.
8. **Tests & CI/CD**: Arreglar tests rotos y pipeline.
