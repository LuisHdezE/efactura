# API Completion Master Matrix

Status: `FULL_OPERATION_LEVEL_RECONCILED / WAVES_1_TO_7_AUDITED / WAVE_1_CLOSED / W2.1_CLOSED / W2.2_IMPLEMENTED_PENDING_MERGE`

Current accepted `main` baseline before PR #216 remains `65 / 194`. PR #216 is the governed W2.2 implementation candidate for `API-SAL-008 cancelSale`; its reconciled candidate baseline is `66 / 194` operations and Wave 2 `24 / 26`. Those candidate counts become the accepted baseline only after final exact-head CI, explicit owner approval and merge.

W2.1 added the bounded read-only `API-SAL-009 getSaleFiscalizationStatus` and is closed after contract lock, implementation, exact-head CI, merge, deployment and bounded read-only production runtime acceptance. Detailed evidence is recorded in `documentation/api-completion-matrix/W2_1_SALE_FISCALIZATION_STATUS_RUNTIME_CLOSURE.md`.

W1.5 Terminals was implemented in PR #195, merged, accepted by post-merge Clean Architecture Guard #675, deployed as Cloud Run revision `efactura-api-d22-11139c7-54-1`, promoted through explicitly approved Neon migration `20260921024500_V1Terminals`, accepted by production runtime run `35612527016`, and independently verified in Neon. Detailed W1.5 evidence is recorded in `documentation/api-completion-matrix/W1_5_TERMINALS_RUNTIME_CLOSURE.md`.

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

- original commercial/fiscal/administrative design: **171** operations;
- Technical Operations Console amendment: **22** operations (`API-172..API-193`);
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

## 3. Current implementation candidate

| Wave | Scope | Operation IDs | Implemented | Missing HTTP | Contract collision | Non-implemented |
|---:|---|---:|---:|---:|---:|---:|
| 1 | Identity + Organization + Reference Data | 30 | 30 | 0 | 0 | 0 |
| 2 | Parties + Catalog + Sales Completion | 26 | 24 | 2 | 0 | 2 |
| 3 | Payments + Cash + AR/AP | 24 | 0 | 24 | 0 | 24 |
| 4 | Inventory + Transfers + Procurement + Receiving | 23 | 4 | 19 | 0 | 19 |
| 5 | Fiscal Completion + CAE + CFE Lifecycle | 40 | 8 | 32 | 0 | 32 |
| 6 | Reporting + Audit + Sync | 21 | 0 | 21 | 0 | 21 |
| 7 | Technical Operations Console | 30 | 0 | 28 | 2 | 30 |
| **Total** |  | **194** | **66** | **126** | **2** | **128** |

Raw candidate operation-ID implementation coverage is `66 / 194 = 34.02%`.

This is an operation-count measure only. It is not a product-readiness score and does not diminish deeper Domain/Application/fiscal capabilities that are not yet exposed through the governed public API. Until PR #216 is merged, the accepted `main` baseline remains `65 / 194`; the table above deliberately tracks the exact implementation candidate under review.

W1.1A contributes `API-REF-002 listUruguayDepartments` and `API-REF-003 listFiscalIdentityTypes`. W1.1B adds `API-REF-001 listCountries` and `API-REF-004 listCurrencies` after closing their governed source prerequisites. W1.1C adds `API-REF-005 listFiscalDocumentTypes` and `API-REF-006 listInvoiceIndicators` after closing fiscal scope with fail-closed Release-1 subsets. W1.1D completes Reference Data with `API-REF-007 listContactTypes` and `API-REF-008 listUnitsOfMeasure`, preserving configurable party-contact semantics and projecting only active commercial units visible through the actor's company scopes. W1.2 adds `API-IAM-001 getCurrentActor` and `API-IAM-011 listPermissions` by projecting the existing actor context and canonical `Permissions.All` set without introducing a second identity model or persistence boundary. W1.3 adds `API-IAM-006..009` through a dedicated company-scoped `SecurityRole` aggregate, canonical permission-code validation, idempotent create/update, optimistic concurrency, audit/outbox evidence and provider-real PostgreSQL/MySQL persistence, and is formally closed after production schema promotion, Cloud Run deployment and runtime acceptance. W1.4 adds `API-IAM-002..005` and `API-IAM-010` through the provider-neutral `SecurityUser` model, organization-scoped user management, idempotent/versioned mutation, scope/self-escalation protections, role replacement and durable audit/outbox evidence. W1.4 is formally closed after PR #185 merge, accepted deployment, explicitly approved production migration, runtime acceptance and independent Neon verification. W1.5 adds the remaining four Wave 1 operations `API-ORG-007..010` through the governed Terminal aggregate, organization/location invariants, idempotent/versioned mutation, durable audit/outbox evidence and provider-neutral PostgreSQL/MySQL persistence. W1.5 is formally closed after PR #195 merge, post-merge Guard #675, accepted deployment, explicitly approved production schema promotion, successful runtime acceptance run `35612527016`, and independent Neon verification. Wave 1 is therefore fully closed at `30 / 30`.

W2.1 began Wave 2 implementation from the owner-locked PR #207 contract. PR #208 exposed `GET /api/v1/sales/{saleId}/fiscalization` as `getSaleFiscalizationStatus` with `sales.read`, using existing Sale, FiscalizationRequest and FiscalDocument readers only. Its projection is explicitly local-workflow authority (`NOT_REQUESTED`, `PENDING`, `IDENTITY_CREATED`) and never claims DGI/provider acceptance or transport state. Impossible persistence combinations fail closed with `fiscalization.inconsistent_state`. No schema or migration was introduced.

W2.1 is formally closed after pre-merge Guard #695, owner-approved PR #208 merge at `9989c0f15d76423f76e25e1cfdc1ab1337586849`, post-merge Guard #696, Deploy API Demo #55, promotion of Cloud Run revision `efactura-api-d22-9989c0f-55-1`, and read-only production runtime acceptance run `35643849728`. Production contained zero Sale rows, so no artificial business fixture was created solely to force a 200 response; successful-state projections remain covered by accepted automated QA.

W2.2 contract authority was owner-locked in PR #213. PR #216 implements `POST /api/v1/sales/{saleId}/cancel` as `cancelSale` with `sales.cancel`, terminal `SaleStatus.Cancelled = 4`, required idempotency, version/reason validation, organization/location/terminal authorization, durable `SALE_CANCELLED` audit, `SaleCancelledIntegrationEvent`, and one local transaction. `CONFIRMED` is the hard irreversible boundary. The slice contains no Payment, Receivable, stock, FiscalizationRequest, FiscalDocument, CAE or DGI/provider dependency and introduces no migration. Detailed candidate evidence is recorded in `documentation/blueprint-api-implementation/72_W2_2_SALE_CANCELLATION.md`.

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
- `IMPLEMENTED_PENDING_MERGE`: the matching HTTP surface exists in an implementation PR while governed merge and later operational gates are pending.
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

## 7. Completion-wave sequencing

Wave 1 closed through bounded increments W1.1 through W1.5 plus runtime reconciliation.

Wave 2 proceeds from `W2_READINESS_AUDIT.md`:

1. `W2.1 API-SAL-009 getSaleFiscalizationStatus`: CLOSED after contract PR #207, implementation PR #208, CI, deployment and read-only runtime acceptance.
2. `W2.2 API-SAL-008 cancelSale`: owner-locked contract merged in PR #213; implementation candidate is PR #216 and remains pending exact-head CI/owner merge/deployment/runtime gates.
3. `W2.3 API-POS-001 getPosBootstrap`: bootstrap composition/freshness contract remains required.
4. `W2.4 API-PTY-008 getPartyAccountSummary`: authoritative party-scoped AR/AP read model remains required.
5. `W2.5`: Wave 2 reconciliation and runtime closure.

The two remaining prerequisite-gated gaps must not be bypassed merely to increase endpoint counts.

## 8. QA rule for completion waves

Every implementation increment must include, as applicable:

- tests for new Application behavior;
- architecture tests protecting dependency direction and public-contract placement;
- CrossCutting/API tests for route, permission/authentication, Problem Details and idempotency obligations;
- provider-real PostgreSQL/MySQL integration tests for persistence/concurrency behavior when persistence changes;
- exact-head Clean Architecture Guard success before merge approval;
- matrix row/status updates in the same governed increment;
- no test weakening to obtain green CI.

## 9. Closed gates and current W2.2 candidate

W1.4 closure evidence is recorded in `documentation/api-completion-matrix/W1_4_USERS_READINESS.md`.

The accepted W1.4 implementation was merged in PR #185 at `5a939ba251a898ea1dd6a181050fffa20e91324d`. Deploy API Demo run `35534908414` promoted Cloud Run revision `efactura-api-d22-5a939ba-53-1`, with canary and public post-promotion smoke PASS. Migration `20260920183000_V1SecurityUsers` was applied to Neon production only after explicit approval, and post-migration schema verification confirmed the four additive W1.4 tables, five indexes, expected PK/FK delete semantics, application grants and EF migration history `8.0.30`.

HTTP runtime acceptance `20260920220501` passed the exact five-operation OpenAPI contract, 401/403 authorization behavior, provider-neutral user creation, deterministic get/list, PATCH update semantics, duplicate identity rejection, idempotent replay/payload mismatch, stale-version rejection, scope validation, inactive-role rejection, role replacement/replay, self-escalation protection, organization isolation and W1.3/W1.2/W1.1/parties regressions.

Independent production Neon verification then confirmed exactly four W1.4 successful user mutation triplets keyed by correlation ID, each containing completed idempotency + successful audit + durable outbox evidence. Failed validation/authorization/conflict paths left no successful durable residue. The runtime QA user finished at version `4` with no residual scopes/roles, and the QA role finished inactive.

W1.5 owns `API-ORG-007..010`. Its accepted contract and final reconciled readiness are recorded in `documentation/api-completion-matrix/W1_5_TERMINALS_READINESS.md`, with operational closure evidence in `documentation/api-completion-matrix/W1_5_TERMINALS_RUNTIME_CLOSURE.md`.

PR #195 implemented the four routes, accepted DTOs, organization authorization, deterministic list/filter behavior, immutable normalized code, optimistic concurrency, idempotency, stable conflict semantics, same-organization active-location validation, reassignment/reactivation rules, the active-terminal dependency invariant for location deactivation, audit/outbox evidence and additive provider-neutral terminal persistence. Post-merge Clean Architecture Guard #675 passed. Cloud Run deployment run `35560340904` accepted revision `efactura-api-d22-11139c7-54-1`. Migration `20260921024500_V1Terminals` was explicitly approved and promoted to Neon production.

Final W1.5 runtime acceptance run `35612527016` passed OpenAPI, authentication/authorization, organization isolation, prerequisite provisioning through public HTTP, Terminal registration/list/get/update behavior, deterministic replay, conflict semantics, location lifecycle invariants, stale-version handling, DELETE absence and Wave 1 regressions. Independent Neon verification confirmed final expected versions, exactly six Terminal audit events, six Terminal outbox messages, six completed Terminal idempotency records, zero non-completed Terminal idempotency residue and zero Terminal evidence in the isolated organization B.

Wave 1 is therefore formally closed at `30 / 30` implemented operations.

W2.1 closure evidence is recorded in `documentation/api-completion-matrix/W2_1_SALE_FISCALIZATION_STATUS_RUNTIME_CLOSURE.md`. Runtime run `35643849728` verified the exact deployed revision, OpenAPI route/operation, GET-only surface, 401 authentication behavior, 403 permission enforcement, 403 organization-scope enforcement and 404 masked unknown-sale behavior with no production writes or fixture creation.

W2.2 currently remains an implementation candidate only. No production Sale creation/cancellation, schema write or runtime mutation is authorized by PR #216. Any mutation-based runtime acceptance after deployment requires a separate explicit owner approval.

Wave 7 retains one explicit prerequisite: resolve the `API-MON-001` / `API-180` contract collision through a separate governed contract decision before implementing either route.
