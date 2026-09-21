# API Completion Master Matrix

Status: `FULL_OPERATION_LEVEL_RECONCILED / WAVES_1_TO_7_AUDITED / W1_4_CLOSED / W1_5_IMPLEMENTED_PRE_MERGE`

Current implementation baseline: PR #195 (`feat/w1-5-terminals`) reconciled with `main@59a286185e242dcd59fb1b946a7d19aae07a54b1`. W1.5 readiness and its owner-approved field-level Terminal contract are closed prerequisites, and the four W1.5 public HTTP operations now exist in the implementation branch. This changes HTTP implementation accounting but does not mean W1.5 is production-accepted: final exact-head CI, governed merge, separately approved schema promotion, deployment and runtime acceptance remain open.

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
| 1 | Identity + Organization + Reference Data | 30 | 30 | 0 | 0 | 0 |
| 2 | Parties + Catalog + Sales Completion | 26 | 22 | 4 | 0 | 4 |
| 3 | Payments + Cash + AR/AP | 24 | 0 | 24 | 0 | 24 |
| 4 | Inventory + Transfers + Procurement + Receiving | 23 | 4 | 19 | 0 | 19 |
| 5 | Fiscal Completion + CAE + CFE Lifecycle | 40 | 8 | 32 | 0 | 32 |
| 6 | Reporting + Audit + Sync | 21 | 0 | 21 | 0 | 21 |
| 7 | Technical Operations Console | 30 | 0 | 28 | 2 | 30 |
| **Total** |  | **194** | **64** | **128** | **2** | **130** |

Raw operation-ID implementation coverage is `64 / 194 = 32.99%`.

This is an operation-count measure only. It is not a product-readiness score and does not diminish deeper Domain/Application/fiscal capabilities that are not yet exposed through the governed public API.

W1.1A contributes `API-REF-002 listUruguayDepartments` and `API-REF-003 listFiscalIdentityTypes`. W1.1B adds `API-REF-001 listCountries` and `API-REF-004 listCurrencies` after closing their governed source prerequisites. W1.1C adds `API-REF-005 listFiscalDocumentTypes` and `API-REF-006 listInvoiceIndicators` after closing fiscal scope with fail-closed Release-1 subsets. W1.1D completes Reference Data with `API-REF-007 listContactTypes` and `API-REF-008 listUnitsOfMeasure`, preserving configurable party-contact semantics and projecting only active commercial units visible through the actor's company scopes. W1.2 adds `API-IAM-001 getCurrentActor` and `API-IAM-011 listPermissions` by projecting the existing actor context and canonical `Permissions.All` set without introducing a second identity model or persistence boundary. W1.3 adds `API-IAM-006..009` through a dedicated company-scoped `SecurityRole` aggregate, canonical permission-code validation, idempotent create/update, optimistic concurrency, audit/outbox evidence and provider-real PostgreSQL/MySQL persistence, and is formally closed after production schema promotion, Cloud Run deployment and runtime acceptance. W1.4 adds `API-IAM-002..005` and `API-IAM-010` through the provider-neutral `SecurityUser` model, organization-scoped user management, idempotent/versioned mutation, scope/self-escalation protections, role replacement and durable audit/outbox evidence. W1.4 is formally closed after PR #185 merge, accepted deployment, explicitly approved production migration, runtime acceptance and independent Neon verification. W1.5 adds the remaining four Wave 1 operations `API-ORG-007..010` through the governed Terminal aggregate, organization/location invariants, idempotent/versioned mutation, durable audit/outbox evidence and provider-neutral PostgreSQL/MySQL persistence. The HTTP surfaces are implemented in PR #195; W1.5 remains open for final exact-head CI, merge, production schema promotion, deployment and runtime acceptance.

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
- historical `W1_4_READY`: the bounded W1.4 readiness audit was locked before implementation; all five rows are now `IMPLEMENTED / EXISTING_PATH / regression` after closure.
- historical `IMPLEMENTATION_READY_PENDING_MERGE`: prerequisite was owner-approved before the bounded W1.5 implementation started.
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
   - `W1.1B`: implemented `API-REF-001` and `API-REF-004` after closing country/currency source prerequisites.
   - `W1.1C`: implemented `API-REF-005` and `API-REF-006` with fail-closed fiscal document/indicator scope and exact `fiscal.read` authorization.
   - `W1.1D`: implemented `API-REF-007` and `API-REF-008` after closing ContactType compatibility and commercial-unit/DGI boundary semantics.
2. `W1.2` Current actor + permission catalog: `API-IAM-001`, `API-IAM-011` — implemented by projecting `IActorContextAccessor.Current` and `Permissions.All`.
3. `W1.3` Roles read/write: `API-IAM-006..009` — implemented, merged, schema-promoted, deployed, runtime-accepted and formally closed.
4. `W1.4` Users + role assignment: `API-IAM-002..005`, `API-IAM-010` — implemented, merged, schema-promoted, deployed, runtime-accepted and formally closed.
5. `W1.5` Terminals: `API-ORG-007..010` — implemented in PR #195 with provider-real persistence QA; final exact-head CI, merge, schema promotion, deployment and runtime acceptance remain.
6. `W1.6` Wave reconciliation: exact contract/implementation/test coverage and documentation closeout after W1.5 runtime closure.

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

## 9. Closed W1.4 gate and W1.5 implementation gate

W1.4 closure evidence is recorded in `documentation/api-completion-matrix/W1_4_USERS_READINESS.md`.

The accepted implementation was merged in PR #185 at `5a939ba251a898ea1dd6a181050fffa20e91324d`. Deploy API Demo run `35534908414` promoted Cloud Run revision `efactura-api-d22-5a939ba-53-1`, with canary and public post-promotion smoke PASS. Migration `20260920183000_V1SecurityUsers` was applied to Neon production only after explicit approval, and post-migration schema verification confirmed the four additive W1.4 tables, five indexes, expected PK/FK delete semantics, application grants and EF migration history `8.0.30`.

HTTP runtime acceptance `20260920220501` passed the exact five-operation OpenAPI contract, 401/403 authorization behavior, provider-neutral user creation, deterministic get/list, PATCH update semantics, duplicate identity rejection, idempotent replay/payload mismatch, stale-version rejection, scope validation, inactive-role rejection, role replacement/replay, self-escalation protection, organization isolation and W1.3/W1.2/W1.1/parties regressions.

Independent production Neon verification then confirmed exactly four W1.4 successful user mutation triplets keyed by correlation ID, each containing completed idempotency + successful audit + durable outbox evidence. Failed validation/authorization/conflict paths left no successful durable residue. The runtime QA user finished at version `4` with no residual scopes/roles, and the QA role finished inactive.

W1.5 owns `API-ORG-007..010`. Its bounded audit and owner-approved Terminal contract are recorded in `documentation/api-completion-matrix/W1_5_TERMINALS_READINESS.md`. PR #195 now implements the four routes, accepted DTOs, organization authorization, deterministic list/filter behavior, immutable normalized code, optimistic concurrency, idempotency, stable conflict semantics, same-organization active-location validation, terminal reassignment/reactivation rules, the active-terminal dependency invariant for location deactivation, audit/outbox evidence and additive provider-neutral terminal persistence. Dedicated architecture and PostgreSQL/MySQL integration tests were added. The migration exists as source only and no production database write is authorized by this implementation state.

W1.5 is not formally closed until the implementation PR is merged, the production schema promotion is separately approved and verified, the accepted API revision is deployed, and HTTP runtime acceptance proves OpenAPI/auth/permission/idempotency/concurrency/location invariants plus required regressions.

Wave 7 retains one explicit prerequisite: resolve the `API-MON-001` / `API-180` contract collision through a separate governed contract decision before implementing either route.
