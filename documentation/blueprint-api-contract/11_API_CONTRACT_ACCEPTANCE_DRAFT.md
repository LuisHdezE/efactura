# API Contract Ready Acceptance Draft

## Current state

`api_contract_ready = READY_FOR_REVIEW`

This boundary does **not** authorize implementation yet.

Blueprint evaluator: `0.5.1@ac8be4e3332b13cab7d27f12e6a62d5d60e9ff4e`.

Initial API Contract design started from accepted `main@c7eebce0826907a619298acc6fcf36aa94846e92`.

The human-accepted Technical Operations Console amendment was subsequently merged in PR #10, producing `main@d2a1d27cda1c2aa173907575e998e67c86ebb800`, and is reconciled into this API Contract before PASS.

## Gate matrix

| Blueprint check | Status | Evidence |
|---|---|---|
| `api.scope_defined` | READY_FOR_REVIEW | `01_API_SCOPE_AND_CONVENTIONS.md`, `13_TECHNICAL_OPERATIONS_CONSOLE_API_CONTRACT.md` |
| `api.endpoint_inventory` | READY_FOR_REVIEW | `02A_ENDPOINT_INVENTORY_CORE.md`, `02B_ENDPOINT_INVENTORY_OPERATIONS.md`, `02C_ENDPOINT_INVENTORY_TECHNICAL_OPERATIONS.md` |
| `api.auth_contract` | READY_FOR_REVIEW | `03_AUTH_AND_PERMISSION_MATRIX.md`, `14_TECHNICAL_OPERATIONS_PERMISSION_AUDIT_IDEMPOTENCY.md` |
| `api.permission_matrix` | READY_FOR_REVIEW | `03_AUTH_AND_PERMISSION_MATRIX.md`, `14_TECHNICAL_OPERATIONS_PERMISSION_AUDIT_IDEMPOTENCY.md` + endpoint inventories |
| `api.audit_event_mapping` | READY_FOR_REVIEW | `04_AUDIT_EVENT_MAPPING.md`, `14_TECHNICAL_OPERATIONS_PERMISSION_AUDIT_IDEMPOTENCY.md` |
| `api.idempotency_matrix` | READY_FOR_REVIEW | `05_IDEMPOTENCY_AND_CONCURRENCY_MATRIX.md`, `14_TECHNICAL_OPERATIONS_PERMISSION_AUDIT_IDEMPOTENCY.md` |
| `api.contract_traceability` | READY_FOR_REVIEW | `09_INTERFACE_SCOPE_RECONCILIATION.md`, `10_API_CONTRACT_TRACEABILITY.md`, `15_TECHNICAL_OPERATIONS_TRACEABILITY_AND_CHANGE_IMPACT.md` |
| `api.change_impact_analysis` | N/A by canonical post-gate rule | initial v1 has not passed API Gate/no accepted executable v1 consumers; Technical Operations pre-gate impact is nevertheless explicitly recorded in `15_TECHNICAL_OPERATIONS_TRACEABILITY_AND_CHANGE_IMPACT.md` |

Supporting contracts:

- `06_PROBLEM_DETAILS_ERROR_CONTRACT.md`
- `07_REQUEST_RESPONSE_CONTRACTS.md`
- `08_BROWNFIELD_COMPATIBILITY_MATRIX.md`
- `12_TECHNICAL_LOGGING_AND_OBSERVABILITY_CONTRACT.md`
- `13_TECHNICAL_OPERATIONS_CONSOLE_API_CONTRACT.md`

The logging/observability contract is included because Blueprint `dev-event-logging-audit` requires a technical logging strategy distinct from durable business/security audit. It preserves the Brownfield Serilog/Application Insights capabilities while requiring end-to-end correlation, structured logging, recursive redaction, worker/integration observability and later QA evidence.

The Technical Operations Console is a normalized operational surface over those observability capabilities. It does not expose raw log files, telemetry credentials, arbitrary backend queries or direct infrastructure mutation.

## Quantitative/structural review

The reconciled design contains **193 unique public v1 operations**, each with a stable API ID and unique intended OpenAPI `operationId`, distributed across business/operational capabilities rather than controller-per-screen design.

- original commercial/fiscal/administrative v1 design: **171** operations;
- accepted Technical Operations amendment: **22** operations (`API-172..API-193`);
- reconciled total: **193** operations.

Base namespace: `/api/v1`.

Technical Operations namespace: `/api/v1/operations/**`.

Legacy route removal authorized: **none**.

Generic client-controlled `issue arbitrary CFE type` endpoint: **none**.

Generic `execute SQL/edit queue row/force business state/browse raw log file` endpoint: **none**.

Specialized fiscal operations with unresolved field-level rules: explicitly `DEFERRED_PENDING_RULES`, not fabricated.

## Human review questions

Before PASS, confirm:

1. the v1 scope matches the intended commercial/fiscal product;
2. the 193-operation surface is coherent and not accidental UI-driven CRUD;
3. permission names and company/location/terminal/object scope are acceptable;
4. no core use case lacks an operation or documented internal workflow;
5. specialized fiscal deferrals are acceptable until their exact DGI slices are complete;
6. JWT Bearer API boundary without invented username/password login matches the intended deployment strategy;
7. Brownfield coexistence/deprecation policy is acceptable;
8. Problem Details, correlation, idempotency and concurrency conventions are acceptable;
9. future web/Android needs are adequately represented without allowing clients to invent business authority;
10. the technical logging strategy is acceptable: Serilog/Application Insights preserved, structured event context, correlation through HTTP/jobs/integrations, no raw sensitive payload logging, independent Class-D retention, and explicit redaction/QA obligations;
11. the Technical Operations Console contract is acceptable: bounded sanitized monitoring, independent `operations.*` permissions, no raw infrastructure access, explicit retry/alert/diagnostic commands, durable audit of privileged actions and provider-independent observability DTOs.

## Stop condition

No C# v1 endpoint implementation, database migration, OpenAPI validation gate or legacy-route removal starts until this API Contract receives explicit human acceptance.

After acceptance, Blueprint permits API Implementation. OpenAPI is then the later machine-readable formalization/validation of this accepted contract and cannot silently redesign it.

## Critical later invariant

Even after implementation becomes functionally correct:

`architecture design acceptance != architecture implementation conformance`

The later `api.architecture_implementation_conformance` check must independently prove that C# code respects the accepted Architecture boundary.
