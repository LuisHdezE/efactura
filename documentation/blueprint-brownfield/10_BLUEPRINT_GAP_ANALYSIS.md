# Blueprint Gap Analysis

This matrix compares the observed AS-IS baseline with applicable Blueprint 0.5.1 expectations. It intentionally includes aligned, partial, gap and unknown items.

| ID | Area | Status | Severity | Classification | Evidence | Impact | External-contract impact | Regression risk | Future recommendation |
|---|---|---|---|---|---|---|---|---|---|
| BG-001 | Secrets | GAP | P0 | OBSERVED | DB/JWT/Blob secrets versioned | credential compromise | indirect | high if rotated poorly | rotate, remove, exposure/history assessment |
| BG-002 | AuthZ | GAP | P1 | OBSERVED | JWT middleware exists; no endpoint authorization enforcement observed | anonymous access risk | high | high | define permission model and enforce policies |
| BG-003 | CORS | GAP | P1 | OBSERVED | any origin/method/header | broad browser cross-origin exposure | medium | medium | explicit environment policy/origins |
| BG-004 | JWT hardening | PARTIAL | P2 | OBSERVED | issuer/audience/lifetime/signing validation present; RequireHttpsMetadata=false | security hardening ambiguity | low/medium | medium | define production authority/TLS/token policy |
| BG-005 | HTTPS | PARTIAL | P2 | OBSERVED/UNKNOWN | UseHttpsRedirection present; deployment TLS/HSTS unknown | transport assurance incomplete | medium | low | production transport policy + tests |
| BG-006 | Project layering | PARTIAL | P2 | OBSERVED | project graph has no cycle | useful clean structure | low | medium | preserve graph while removing core framework leakage |
| BG-007 | Core purity | GAP | P2 | OBSERVED | ApplicationCore references EF Core/ASP.NET abstractions | architecture drift | low | medium | move framework details outward incrementally |
| BG-008 | Shared boundary | PARTIAL | P2 | OBSERVED | Shared references Core and contains web/security/observability dependencies | unclear boundary | low | medium | narrow responsibility/ADR |
| BG-009 | Active persistence | ALIGNED | P3 | OBSERVED | PostgreSQL active via Npgsql/EF/Dapper | functional data access exists | low | medium | preserve during refactoring |
| BG-010 | Multi-provider packages | UNKNOWN | P3 | OBSERVED | MySQL/Oracle/SQLServer packages exist, active use unproven | dependency clutter/history unclear | none | low | classify/remove only after target decision |
| BG-011 | Data-model duplication | GAP | P2 | OBSERVED | singular/plural model families + namespace split | ambiguous ownership/mapping | medium | high | usage-driven reconciliation, no blind deletion |
| BG-012 | DB portability | GAP | P2 | OBSERVED | PostgreSQL sequence/default syntax in DBContext | current model tied to PostgreSQL | none AS-IS | high | provider-neutral target + parity tests if approved |
| BG-013 | Relationships | UNKNOWN | P2 | OBSERVED/INFERRED | scalar FK-like fields, no relationship config observed in DBContext | integrity model unclear | medium | high | inspect DB constraints/scripts before redesign |
| BG-014 | API inventory | PARTIAL | P2 | OBSERVED | 69 CRUD/info endpoints exist, no versioned API contract | inconsistent client contract | high | high | preserve/impact-analyze before v1 redesign |
| BG-015 | HTTP error contract | GAP | P2 | OBSERVED | ResultObject false mostly maps to 400; not-found handler logical 404 -> HTTP400 | ambiguous client behavior | high | medium | Problem Details/stable codes after compatibility review |
| BG-016 | OpenAPI | PARTIAL | P2 | OBSERVED | Swashbuckle exists, no accepted authoritative contract | tooling without governance | high | medium | reconstruct/validate formal OpenAPI |
| BG-017 | Business use cases | GAP | P1 | OBSERVED | current API is mostly master CRUD; no full POS/CFE/AR/AP flows | product incomplete | high/new scope | high | target requirements before implementation |
| BG-018 | Durable audit | GAP | P1 | OBSERVED | technical logs only, no durable business audit observed | non-repudiation/traceability gap | medium | medium | event catalog + durable audit store |
| BG-019 | Observability | PARTIAL | P2 | OBSERVED | Serilog/AppInsights configured | baseline operational visibility | low | medium | correlation/actor enrichment, metrics/traces |
| BG-020 | Tests | PARTIAL | P2 | OBSERVED | 20 pass, 1 fail; narrow service tests | regression safety limited | medium | high | fix baseline test later; add architecture/API/persistence/security suites |
| BG-021 | Docker | GAP | P2 | OBSERVED | Docker .NET 6 vs projects .NET 8 | build/deploy mismatch | none | medium | align container after evidence approval |
| BG-022 | Azure CI | PARTIAL | P2 | OBSERVED | restore/build/test pipeline exists | useful baseline but limited | none | low | modernize around actual toolchain |
| BG-023 | GitLab CI | GAP | P3 | OBSERVED | echo-only jobs | false CI signal | none | low | remove or implement based on chosen platform |
| BG-024 | Documentation | GAP | P3 | OBSERVED | README runtime/db/architecture drift | onboarding/governance confusion | none | low | replace template assumptions with product docs |
| BG-025 | Service logic | UNKNOWN/PARTIAL | P2 | OBSERVED | CustomerService pass-through sample | rule ownership unclear | medium | medium | assess each future domain before refactor |
| BG-026 | Idempotency | GAP | P1 future-critical | OBSERVED | no replay/idempotency mechanism observed | duplicate financial/fiscal effects in target | high | high | required before retry-sensitive target commands |
| BG-027 | Rate limiting | GAP | P2 future-critical | OBSERVED absence | no API abuse control observed | medium | low | define per-route policy |
| BG-028 | Fiscal integration | GAP | P1 product scope | OBSERVED | no DGI/CFE lifecycle in current API | core target capability absent | new contract | high | requirements/regulatory design first |
| BG-029 | Architecture implementation check | PARTIAL | P2 | OBSERVED | layered intent exists but no fitness tests | drift can recur | none | low | executable architecture tests later |
| BG-030 | Technology preservation | ALIGNED | P3 | PROPOSED constraint grounded in AS-IS | existing .NET structure is viable evolution base | none | low | evolve, do not rewrite |

## Priority interpretation

- `P0`: containment/security exposure before broad implementation.
- `P1`: security/product-integrity boundaries required before financial/fiscal production use.
- `P2`: architecture/contract/testing/delivery hardening.
- `P3`: lower operational/documentation cleanup.

Gap existence does not authorize implementation. Remediation order is defined separately and must preserve compatibility or explicitly analyze API impact.
