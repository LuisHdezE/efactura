# Brownfield Executive Summary

## Baseline

- eFactura main inspected: `a6c9bf96572b8a0a88efde2c68b0749a71020a18`
- Blueprint evaluator: `0.5.1@ac8be4e3332b13cab7d27f12e6a62d5d60e9ff4e`
- Stack preserved: .NET/C#/ASP.NET Core; PostgreSQL is the active observed database path.

## What exists today

The solution has a useful layered .NET 8 skeleton with ApplicationCore, Shared, Infrastructure, WebApi and UnitTest. Dapper/Npgsql and EF Core/PostgreSQL are active, Redis/Serilog/Application Insights/Swagger/JWT are configured, and entity-oriented services/repositories exist.

However, current executable functionality is predominantly master-data CRUD rather than a complete electronic-invoicing platform.

### Measured inventory

- **16** controller files
- **14** controllers with actions
- **69** HTTP actions
- **24** entity/model files under ApplicationCore/Entities
- **12** DBContext DbSets
- **21** tests executed: 20 pass, 1 fail

## Highest-priority findings

### P0

Versioned secrets/credentials: PostgreSQL, JWT signing and Azure Blob categories. Values remain redacted in evidence.

### P1

- JWT authentication infrastructure exists, but no endpoint authorization enforcement was observed.
- no durable business/security audit mechanism observed;
- future financial/fiscal expansion requires idempotency/replay protection not present today;
- current system does not implement the required POS/CFE/fiscal business lifecycles.

### P2

- ApplicationCore directly references EF Core and ASP.NET abstractions despite declared core independence;
- singular/plural model families coexist with namespace/schema drift;
- DBContext uses PostgreSQL-specific sequence/default syntax;
- error contract collapses many failures to 400 and has 404/400 inconsistency;
- Docker remains .NET 6 while projects are .NET 8;
- testing is narrow and one current unit test fails.

### P3

README/setup and historical CI documentation are substantially stale; GitLab pipeline is placeholder-only.

## Corrected hypothesis validation

| Hypothesis | Status | Corrected interpretation |
|---|---|---|
| HYP-001 secrets exposed | CONFIRMED | P0; redact, rotate/remove/history-assess later. |
| HYP-002 Docker 6 vs net8 | CONFIRMED | runtime/container drift. |
| HYP-003 ApplicationCore framework dependencies | CONFIRMED | EF Core + ASP.NET abstractions conflict with declared independence. |
| HYP-004 duplicate/parallel models | CONFIRMED | multiple singular/plural families; historical intent UNKNOWN. |
| HYP-005 services are pass-through | PARTIALLY_CONFIRMED | CustomerService confirmed; not proven for every service. |
| HYP-006 404 logical vs HTTP400 | CONFIRMED | global handler inconsistency. |
| HYP-007 permissive CORS | CONFIRMED | broad cross-origin exposure; do not simplistically label as CSRF. |
| HYP-008 RequireHttpsMetadata=false | CONFIRMED CONFIG | hardening concern; HTTPS redirection is also present. |
| HYP-009 no durable audit | CONFIRMED AS OBSERVATION | no durable audit mechanism observed; technical logging exists. |
| HYP-010 GitLab CI placeholder | CONFIRMED | echo-only jobs. |
| HYP-011 README drift | CONFIRMED | runtime/database/architecture/tooling claims diverge. |
| HYP-012 test coverage weak | PARTIALLY_CONFIRMED | narrow test subject set and absent integration/security/architecture suites; not proven by test-count/entity-count arithmetic. |

## Brownfield conclusion

The system is a viable evolutionary base, not a rewrite candidate. The correct strategy is:

`UNDERSTAND -> ACCEPT AS-IS -> ACCEPT TARGET/REQUIREMENTS -> REMEDIATE IN SMALL BOUNDARIES -> IMPLEMENT NEW CAPABILITIES`.

Detailed new-product/fiscal capabilities are intentionally kept on `blueprint/target-functional-reconstruction`; they are not retroactively represented as current behavior.

## Human decision boundary

This hardened evidence is ready for review. No production remediation, Blueprint adoption or Blueprint Master change is implied by publication of these documents.
