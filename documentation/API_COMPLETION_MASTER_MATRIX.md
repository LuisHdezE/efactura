# API Completion Master Matrix

Status: `FULL_OPERATION_LEVEL_RECONCILED / WAVES_1_TO_7_AUDITED / W1_1A_REFERENCE_DATA_FOUNDATION`

Baseline source: W1.1A branch created from `main@bcefca379158d951e59ad6cf1ee69247d92df010`.

This document is the governed index for the public v1 API completion program after D2 backend operational closure. The operation-level matrix is physically split into seven wave shards under `documentation/api-completion-matrix/`, but those shards are one logical matrix and are validated together by `ApiCompletionMasterMatrixArchitectureTests`.

It does not replace the accepted API contract inventories. It reconciles them with the current WebApi implementation and makes gaps explicit without inventing implementation readiness.

## 1. Authoritative inputs

- `documentation/blueprint-api-contract/02A_ENDPOINT_INVENTORY_CORE.md`
- `documentation/blueprint-api-contract/02B_ENDPOINT_INVENTORY_OPERATIONS.md`
- `documentation/blueprint-api-contract/02C_ENDPOINT_INVENTORY_TECHNICAL_OPERATIONS.md`
- `documentation/blueprint-api-contract/11_API_CONTRACT_ACCEPTANCE_DRAFT.md`
- current `src/WebApi/Controllers/V1/**` implementation
- `documentation/API_COMPLETION_MASTER_MATRIX_WAVE_COUNTS.md`
- `documentation/api-completion-matrix/WAVE_1.md` through `WAVE_7.md`

No endpoint is considered implemented merely because Domain/Application capability exists. `IMPLEMENTED` requires a current public v1 HTTP surface matching the governed method/path and permission boundary.

## 2. 193 vs 194 chronology

The historical API Contract Ready acceptance records:

- original commercial/fiscal/administrative design: `171` operations;
- Technical Operations Console amendment: `22` operations (`API-172..API-193`);
- historical accepted total: `193` operations.

The current inventories contain:

- `02A_ENDPOINT_INVENTORY_CORE.md`: `79` operations;
- `02B_ENDPOINT_INVENTORY_OPERATIONS.md`: `93` operations;
- `02C_ENDPOINT_INVENTORY_TECHNICAL_OPERATIONS.md`: `22` operations;
- current inventory total: **194 operations**.

The legitimate later `+1` is:

- `API-FIS-010`
- `collectFiscalEnvelopeDocumentResponseEvidence`
- `POST /api/v1/fiscal-envelopes/{envelopeId}/document-response-evidence`

`API-FIS-010` has current inventory, WebApi controller, Application composition and automated tests. The completion program therefore uses **194** as the current operation-ID denominator while preserving **193** as the historical accepted count that existed before this later fiscal operation. No existing API ID is renumbered to hide this chronology.

## 3. Current implementation baseline

| Wave | Scope | Operation IDs | Implemented | Missing HTTP | Contract collision | Non-implemented |
|---:|---|---:|---:|---:|---:|---:|
| 1 | Identity + Organization + Reference Data | 30 | 9 | 21 | 0 | 21 |
| 2 | Parties + Catalog + Sales Completion | 26 | 22 | 4 | 0 | 4 |
| 3 | Payments + Cash + AR/AP | 24 | 0 | 24 | 0 | 24 |
| 4 | Inventory + Transfers + Procurement + Receiving | 23 | 4 | 19 | 0 | 19 |
| 5 | Fiscal Completion + CAE + CFE Lifecycle | 40 | 8 | 32 | 0 | 32 |
| 6 | Reporting + Audit + Sync | 21 | 0 | 21 | 0 | 21 |
| 7 | Technical Operations Console | 30 | 0 | 28 | 2 | 30 |
| **Total** |  | **194** | **43** | **149** | **2** | **151** |

Raw operation-ID implementation coverage is `43 / 194 = 22.16%`.

This is an operation-count measure only. It is not a product-readiness score and does not diminish deeper Domain/Application/fiscal capabilities that are not yet exposed through the governed public API.

W1.1A contributes exactly two new public operations: `API-REF-002 listUruguayDepartments` and `API-REF-003 listFiscalIdentityTypes`. The other six Reference Data operations remain non-implemented until their audited prerequisites are closed.

## 4. Logical matrix shards

| Wave | Operation-level source | Expected rows |
|---:|---|---:|
| 1 | `documentation/api-completion-matrix/WAVE_1.md` | 30 |
| 2 | `documentation/api-completion-matrix/WAVE_2.md` | 26 |
| 3 | `documentation/api-completion-matrix/WAVE_3.md` | 24 |
| 4 | `documentation/api-completion-matrix/WAVE_4.md` | 23 |
| 5 | `documentation/api-completion-matrix/WAVE_5.md` | 40 |
| 6 | `documentation/api-completion-matrix/WAVE_6.md` | 21 |
| 7 | `documentation/api-completion-matrix/WAVE_7.md` | 30 |
| **Total** |  | **194** |

CI validates that the shards contain every current inventory API ID exactly once and that each row preserves the accepted `operationId`, HTTP method, route and permission.

## 5. Status vocabulary

- `IMPLEMENTED`: governed HTTP surface exists and matches the current contract boundary.
- `MISSING_HTTP`: accepted operation exists in the contract but no matching public v1 endpoint exists.
- `CONTRACT_COLLISION`: accepted operation cannot safely be implemented as written because its method/path conflicts with another accepted operation.
- `PARTIAL`: an HTTP surface exists but one or more governed obligations are materially incomplete.
- `DEFERRED_PENDING_RULES`: explicitly deferred by the accepted contract.
- `REVIEW_REQUIRED`: evidence is insufficient for a safe classification.

Deep-readiness markers are deliberately separate from HTTP status:

- `EXISTING_PATH / regression`: current public surface exists; preserve and regression-test it.
- `NOT_YET_AUDITED`: do not assume Application, persistence or test readiness from the API contract alone.
- `PREREQUISITE_REQUIRED`: bounded audit found a concrete prerequisite that must close before HTTP implementation.
- `BLOCKED_BY_CONTRACT`: contract reconciliation must occur before implementation.

## 6. Contract collision audit

The full inventory contains **194 unique operation IDs but 193 distinct HTTP method/path signatures**.

The single known collision is:

| API ID | operationId | Signature | Permission |
|---|---|---|---|
| `API-MON-001` | `getIntegrationStatus` | GET `/api/v1/operations/integrations` | `operations.read` |
| `API-180` | `listOperationalIntegrations` | GET `/api/v1/operations/integrations` | `operations.integrations.read` |

Both rows remain represented in Wave 7 and are marked `CONTRACT_COLLISION / BLOCKED_BY_CONTRACT`. This matrix does **not** select a winner, delete an accepted operation, rename a route or renumber an ID. A separate governed contract-reconciliation increment is required before either colliding operation is implemented.

## 7. Wave 1 bounded implementation order

1. `W1.1` Reference Data read-only foundation: `API-REF-001..008`.
   - `W1.1A`: implemented `API-REF-002` and `API-REF-003` from governed source-ready metadata.
   - `W1.1B`: close country/currency source prerequisites for `API-REF-001` and `API-REF-004`.
   - `W1.1C`: close fiscal document/indicator scope for `API-REF-005` and `API-REF-006`.
   - `W1.1D`: close ContactType/UOM semantics for `API-REF-007` and `API-REF-008`.
2. `W1.2` Current actor + permission catalog: `API-IAM-001`, `API-IAM-011`.
3. `W1.3` Roles read/write: `API-IAM-006..009`.
4. `W1.4` Users + role assignment: `API-IAM-002..005`, `API-IAM-010`.
5. `W1.5` Terminals: `API-ORG-007..010`.
6. `W1.6` Wave reconciliation: exact contract/implementation/test coverage and documentation closeout.

Before each bounded implementation increment, the affected rows receive a deeper readiness audit covering Application use cases, persistence, permission enforcement and tests. `NOT_YET_AUDITED` is never treated as implementation readiness.

## 8. QA rule for completion waves

Every implementation increment must include, as applicable:

- tests for new Application behavior;
- architecture tests protecting dependency direction and public-contract placement;
- CrossCutting/API tests for route, permission/authentication, Problem Details and idempotency obligations;
- provider-real PostgreSQL/MySQL integration tests for persistence/concurrency behavior when persistence changes;
- exact-head Clean Architecture Guard success before merge approval;
- matrix row/status updates in the same governed increment;
- no test weakening to obtain green CI.

## 9. Next gate

W1.1A is the first executable Reference Data slice. It introduces no persistence and no direct dependency on the legacy `ApplicationCore`/Npgsql/Dapper reference repositories. After its governed merge and demo deployment, the next Reference Data work is prerequisite closure for W1.1B/C/D before those six routes can move from `MISSING_HTTP`.

Wave 7 retains one explicit prerequisite: resolve the `API-MON-001` / `API-180` contract collision through a separate governed contract decision before implementing either route.
