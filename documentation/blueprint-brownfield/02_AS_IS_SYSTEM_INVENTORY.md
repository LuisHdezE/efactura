# AS-IS System Inventory

Baseline: `efactura@a6c9bf96572b8a0a88efde2c68b0749a71020a18`.

## Solution projects

| Project | Target | Nullable | LangVersion | Project references | Observed responsibility |
|---|---|---|---|---|---|
| `ApplicationCore` | net8.0 | disable | preview | none | Entities, VOs, interfaces, services, helpers; declared core but contains framework dependencies. |
| `Shared` | net8.0 | enable | preview | ApplicationCore | Exception filter plus technical/security/logging/cache dependencies. |
| `Infrastructure` | net8.0 | disable | preview | ApplicationCore, Shared | Repositories, EF context, Dapper/Npgsql access, cache/infrastructure details. |
| `WebApi` | net8.0 | enable | preview | ApplicationCore, Infrastructure, Shared | ASP.NET Core HTTP entry point and composition root. |
| `UnitTest` | net8.0 | enable | default | ApplicationCore | xUnit unit tests for a small subset of services. |

## Runtime/dependency inventory

### ApplicationCore

Observed package families include AutoMapper, NodaTime, ZXing, Newtonsoft.Json, configuration abstractions, **Microsoft.EntityFrameworkCore 8.0.10** and legacy **Microsoft.AspNetCore.Http.Abstractions 2.2.0**.

### Infrastructure

Observed package families include Dapper, Npgsql/PostgreSQL EF providers, `MySql.Data`, Oracle EF Core, SQL Server/System.Data.SqlClient, EF Core, DirectoryServices and Google auth support.

### Shared

Observed package families include Auth0 Authentication API, Serilog + sinks/enrichers, Application Insights, Polly, Redis, JWT bearer and ASP.NET JSON/configuration packages.

### WebApi

Observed package families include Swashbuckle, JWT bearer, Npgsql/PostgreSQL EF provider, EF tools, Application Insights and Google auth ASP.NET support.

## Persistence/provider classification

| Technology/provider | Classification | Evidence |
|---|---|---|
| PostgreSQL / Npgsql | `ACTIVE_OBSERVED` | `Program.cs` registers DBContext with `UseNpgsql`; Dapper repositories instantiate `NpgsqlConnection`; active connection key is Postgres-oriented. |
| Entity Framework Core | `ACTIVE_OBSERVED` | DBContext is registered and mapped; EF packages active in solution. |
| Dapper | `ACTIVE_OBSERVED` | Repository implementations execute parameterized SQL through Dapper. |
| Redis | `ACTIVE_CONFIGURED` | StackExchangeRedisCache registered and cache service DI configured. Runtime connectivity was not independently proven by repository inspection. |
| MySQL | `PACKAGE_PRESENT_NOT_PROVEN_ACTIVE` | `MySql.Data` package exists, but no active composition/persistence path was observed. |
| SQL Server | `PACKAGE_PRESENT_NOT_PROVEN_ACTIVE` | EF SqlServer/System.Data.SqlClient packages and historical README references exist, but active runtime uses Npgsql. |
| Oracle | `PACKAGE_PRESENT_NOT_PROVEN_ACTIVE` | Oracle EF Core package exists; active use not observed. |
| Auth0 | `PACKAGE/CONFIG_REFERENCE_NOT_PROVEN_ACTIVE` | Auth0 package/README references exist; current Program configures local symmetric JWT validation instead. |
| Google auth | `PACKAGE_PRESENT_NOT_PROVEN_ACTIVE` | packages exist; active authentication flow not observed in Program/controllers. |

## Composition-root observations

`WebApi/Program.cs` registers controllers, global exception filter, JWT bearer authentication, CORS, Swagger bearer metadata, Serilog, Application Insights, response compression, Redis, AutoMapper, PostgreSQL DBContext and service/repository pairs for CustomerType, ContactType, ContactDetail, Country, Customer, Department, PaymentMethod, InvoiceIndicator, Products, ProductCategory, DocumentType, Supplier and VoucherType.

`SupplierTypeController` and `TaxTypeController` are empty and no corresponding active controller workflow is exposed.

## Functional AS-IS summary

The current executable API is predominantly **master-data CRUD**. It exposes customers, suppliers, products, categories, lookup/configuration masters and API info. Although accounting-shaped persistence models such as `Invoices`, `Payments`, `PurchaseOrders`, `CashTransactions` and `TaxTypes` exist, the current Web API does not expose a complete sales/POS/CFE/receivables/payables/fiscal lifecycle.

This distinction is critical: data-model presence is not equivalent to implemented business capability.
