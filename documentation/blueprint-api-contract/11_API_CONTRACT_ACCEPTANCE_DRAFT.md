# API Contract Ready Acceptance Draft

## Current state

`api_contract_ready = READY_FOR_REVIEW`

This boundary does **not** authorize implementation yet.

Blueprint evaluator: `0.5.1@ac8be4e3332b13cab7d27f12e6a62d5d60e9ff4e`.

API Contract design started from accepted `main@c7eebce0826907a619298acc6fcf36aa94846e92`.

## Gate matrix

| Blueprint check | Status | Evidence |
|---|---|---|
| `api.scope_defined` | READY_FOR_REVIEW | `01_API_SCOPE_AND_CONVENTIONS.md` |
| `api.endpoint_inventory` | READY_FOR_REVIEW | `02A_ENDPOINT_INVENTORY_CORE.md`, `02B_ENDPOINT_INVENTORY_OPERATIONS.md` |
| `api.auth_contract` | READY_FOR_REVIEW | `03_AUTH_AND_PERMISSION_MATRIX.md` |
| `api.permission_matrix` | READY_FOR_REVIEW | `03_AUTH_AND_PERMISSION_MATRIX.md` + endpoint inventories |
| `api.audit_event_mapping` | READY_FOR_REVIEW | `04_AUDIT_EVENT_MAPPING.md` |
| `api.idempotency_matrix` | READY_FOR_REVIEW | `05_IDEMPOTENCY_AND_CONCURRENCY_MATRIX.md` |
| `api.contract_traceability` | READY_FOR_REVIEW | `09_INTERFACE_SCOPE_RECONCILIATION.md`, `10_API_CONTRACT_TRACEABILITY.md` |
| `api.change_impact_analysis` | N/A | initial v1 API baseline has not passed API Gate and has no accepted v1 consumers yet |

Supporting contracts:

- `06_PROBLEM_DETAILS_ERROR_CONTRACT.md`
- `07_REQUEST_RESPONSE_CONTRACTS.md`
- `08_BROWNFIELD_COMPATIBILITY_MATRIX.md`

## Quantitative/structural review

The current design contains **171 unique public v1 operations**, each with a stable API ID and unique intended OpenAPI `operationId`, distributed across business capabilities rather than controller-per-screen design.

Base namespace: `/api/v1`.

Legacy route removal authorized: **none**.

Generic client-controlled `issue arbitrary CFE type` endpoint: **none**.

Specialized fiscal operations with unresolved field-level rules: explicitly `DEFERRED_PENDING_RULES`, not fabricated.

## Human review questions

Before PASS, confirm:

1. the v1 scope matches the intended commercial/fiscal product;
2. the 171-operation surface is coherent and not accidental UI-driven CRUD;
3. permission names and company/location/terminal/object scope are acceptable;
4. no core use case lacks an operation or documented internal workflow;
5. specialized fiscal deferrals are acceptable until their exact DGI slices are complete;
6. JWT Bearer API boundary without invented username/password login matches the intended deployment strategy;
7. Brownfield coexistence/deprecation policy is acceptable;
8. Problem Details, correlation, idempotency and concurrency conventions are acceptable;
9. future web/Android needs are adequately represented without allowing clients to invent business authority.

## Stop condition

No C# v1 endpoint implementation, database migration, OpenAPI validation gate or legacy-route removal starts until this API Contract receives explicit human acceptance.

After acceptance, Blueprint permits API Implementation. OpenAPI is then the later machine-readable formalization/validation of this accepted contract and cannot silently redesign it.

## Critical later invariant

Even after implementation becomes functionally correct:

`architecture design acceptance != architecture implementation conformance`

The later `api.architecture_implementation_conformance` check must independently prove that C# code respects the accepted Architecture boundary.