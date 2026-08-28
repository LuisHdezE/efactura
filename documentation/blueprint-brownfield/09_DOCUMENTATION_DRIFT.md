# Documentation Drift

Baseline comparison of `README.md`/repository documentation against observed implementation.

| Documentation claim | Observed implementation | Status | Impact |
|---|---|---|---|
| Header/structure says .NET 8 template | all main projects target net8.0 | MATCH | positive |
| Technology list says `.NET 6.0.4 LTS` | csproj targets net8.0 | DRIFT | setup/version confusion |
| Technology list says EF Core 6.0.4 / feature text says EF6 | packages target EF Core 8.0.10 | DRIFT | inaccurate dependency guidance |
| ApplicationCore should be independent | directly references EF Core and ASP.NET Http abstractions | DRIFT | architecture conformance risk |
| Shared is reusable shared concern, implied usable by Core | actual ProjectReference is Shared -> ApplicationCore; Core does not reference Shared | DRIFT/AMBIGUOUS | misleading architecture diagram |
| Auth0 configured as identity provider | Auth0 package/reference exists, but Program actively configures symmetric JWT bearer; no active Auth0 login flow observed | DRIFT/UNKNOWN | security-model ambiguity |
| SQL Server setup/SSMS/bacpac restoration | active `Program.cs` uses Npgsql/PostgreSQL and Dapper repositories use Npgsql | DRIFT | developers can provision wrong database |
| SQL Server generic repository described | current repo also contains Dapper/Npgsql paths | PARTIAL/DRIFT | persistence model unclear |
| Controllers should use REST-based routes | Customer/Products/CustomerType use action-named/query routes and DELETE body/query patterns | DRIFT | inconsistent public API contract |
| Service layer is principal business-logic location | sampled CustomerService is pass-through to repository | PARTIAL/UNKNOWN | business-rule ownership unclear |
| Exceptions always return ResultObject/500 | controllers commonly translate service false to 400; exception handlers have mixed status semantics | DRIFT | client error-contract uncertainty |
| Serilog file + AppInsights logging | configured in Program.cs | MATCH | positive |
| Redis configured | distributed cache configured | MATCH/configured | runtime connectivity still unproven |
| Swagger with authentication | Bearer scheme metadata configured | MATCH_PARTIAL | metadata exists; endpoint AuthZ enforcement does not |
| Changelog/version maintenance | README is substantially stale relative to runtime | DRIFT | governance/doc debt |

## Conclusion

README is currently closer to a historical reusable template than authoritative product documentation. It must not be used as the sole source of architecture/security/runtime truth.
