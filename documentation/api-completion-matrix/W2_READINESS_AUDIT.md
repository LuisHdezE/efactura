# Wave 2 readiness audit

Status: `READINESS_AUDIT_COMPLETE / IMPLEMENTATION_NOT_AUTHORIZED`

Baseline: `main@6ecbb26db8f769ebcd5b9b42cae59633b46ef96f`.

Scope: the four currently non-implemented Wave 2 operations only:

- `API-PTY-008 getPartyAccountSummary`;
- `API-POS-001 getPosBootstrap`;
- `API-SAL-008 cancelSale`;
- `API-SAL-009 getSaleFiscalizationStatus`.

This audit is read-only architectural evidence. It does not authorize implementation, production schema writes, deployment, or runtime mutations.

## 1. Current Wave 2 accounting

Wave 2 currently contains `26` operation IDs:

- `22` implemented public v1 HTTP operations;
- `4` `MISSING_HTTP` operations;
- `0` contract collisions.

No current implemented route is reclassified by this audit.

## 2. Readiness classification

| API ID | operationId | Current status | Readiness result | Concrete prerequisite |
|---|---|---|---|---|
| `API-SAL-009` | `getSaleFiscalizationStatus` | `MISSING_HTTP` | `IMPLEMENTATION_READY_PENDING_CONTRACT_LOCK` | lock the exact read projection and safe external-state semantics |
| `API-POS-001` | `getPosBootstrap` | `MISSING_HTTP` | `PREREQUISITE_REQUIRED` | lock bootstrap DTO/composition/freshness contract and dependency set |
| `API-SAL-008` | `cancelSale` | `MISSING_HTTP` | `PREREQUISITE_REQUIRED` | lock cancel lifecycle and irreversible-boundary policy before adding a domain transition |
| `API-PTY-008` | `getPartyAccountSummary` | `MISSING_HTTP` | `PREREQUISITE_REQUIRED` | introduce an authoritative party-scoped balance/aging read model rather than derive financial truth from Party master data |

The recommended first bounded increment is therefore `W2.1 = API-SAL-009` after owner approval of its field-level contract.

## 3. API-SAL-009 — getSaleFiscalizationStatus

Accepted public surface:

```text
GET /api/v1/sales/{saleId}/fiscalization
permission: sales.read
idempotency: NO
```

### Existing evidence

The repository already contains the core authoritative state needed for this read path:

- `FiscalizationRequest` is a persisted domain model keyed to `SaleId`;
- `FiscalizationRequestStatus` currently exposes `Pending` and `IdentityCreated` only;
- `IFiscalizationRequestRepository` / `EfFiscalizationRequestRepository` already support sale-scoped lookup through `GetBySaleAsync(organizationId, saleId)`;
- sale confirmation already creates a durable fiscalization request and returns `FiscalizationRequestId`;
- `FiscalDocument` can be resolved through the fiscalization request/document relationship;
- the current fiscal workflow intentionally distinguishes local workflow state from external DGI acceptance.

This means no new aggregate or schema is required merely to expose the current local fiscalization workflow state.

### Contract boundary that must be locked

The HTTP projection must not imply facts that the current domain does not know. In particular:

- `Pending` must not be presented as DGI pending/received/accepted;
- `IdentityCreated` means local fiscal-document identity exists, not that XML was signed, transmitted, accepted, rejected, or observed by DGI;
- external raw CFE/DGI evidence must remain separate and only be surfaced when the repository has explicit authoritative evidence;
- organization isolation must be preserved and cross-organization sale IDs must not leak existence.

The field-level DTO, not-found semantics, optional fiscal-document identity fields and status vocabulary require an owner-approved contract lock before implementation.

## 4. API-SAL-008 — cancelSale

Accepted public surface:

```text
POST /api/v1/sales/{saleId}/cancel
permission: sales.cancel
idempotency: REQUIRED
```

The accepted inventory says cancellation is permitted only before an irreversible boundary when policy permits.

### Current domain gap

`SaleStatus` currently contains only:

- `Draft`;
- `Validated`;
- `Confirmed`.

There is no `Cancelled` state and `Sale` has no cancel transition.

`ConfirmSaleUseCase` is already a strong irreversible boundary because confirmation atomically creates/commits local durable effects including settlement evidence, payment and/or receivable consequences, stock effects where applicable, and a fiscalization request. A cancel implementation must therefore not be invented as a controller-only status flip and must never silently reverse confirmed effects.

### Required contract/policy decisions

Before implementation, lock at least:

- whether both `Draft` and `Validated` are cancellable;
- whether `Confirmed` is categorically non-cancellable through this endpoint;
- whether cancellation is terminal and whether a cancelled sale remains queryable;
- request fields such as `expectedVersion`, operator reason/context, and any mandatory reason code;
- stable conflict code when the irreversible boundary has been crossed;
- idempotency scope and replay semantics;
- audit/outbox event names and durable evidence;
- whether cancellation increments version and clears validation evidence or preserves it as historical evidence.

No implementation should begin until this lifecycle contract is explicitly approved.

## 5. API-POS-001 — getPosBootstrap

Accepted public surface:

```text
GET /api/v1/pos/bootstrap
permission: sales.read
idempotency: NO
```

The accepted inventory describes it only as a compact POS bootstrap/cache dataset with freshness metadata.

### Existing foundations

Wave 1 and current Wave 2 already provide substantial ingredients:

- company/location/terminal organization context;
- terminal master data from completed W1.5;
- item/category/tax-profile read surfaces;
- party/customer read surfaces;
- sale creation/validation/confirmation paths;
- reference data used by the POS flow.

However, there is no current `PosController`, Application POS bootstrap use case, or accepted field-level bootstrap projection.

The UI reconciliation also treats target payment-method support as a separate missing dependency rather than fabricating it from legacy metadata.

### Required contract decisions

Before implementation, lock:

- exact bootstrap sections and DTO fields;
- which datasets are mandatory versus optional;
- organization/location/terminal selection semantics;
- whether payment methods are part of the bootstrap payload or remain fetched separately through the governed payment-method API;
- freshness/version metadata and cache invalidation semantics;
- maximum bounded result sizes and whether catalog data is complete or summarized;
- behavior when one optional dependency is unavailable.

The endpoint should compose existing authoritative readers, not create a second source of truth.

## 6. API-PTY-008 — getPartyAccountSummary

Accepted public surface:

```text
GET /api/v1/parties/{partyId}/account-summary
permission: parties.read
idempotency: NO
```

The accepted inventory requires a server-authoritative commercial balance/aging summary.

### Existing evidence and remaining gap

The repository contains a real `Receivable` domain model and persistence repository, and sale confirmation can create receivable effects. That is useful foundation, but it is not yet an account-summary query model:

- the current receivable repository reads by receivable ID or sale ID, not by Party with aging aggregation;
- authoritative outstanding balance requires collections/adjustments/reversals to be reflected rather than summing original sale receivables;
- the earlier Parties implementation explicitly deferred this endpoint until Receivables/Payables projections exist and forbids faking it from Party master data;
- supplier/payable semantics must not be guessed from customer receivable data.

### Required prerequisite

Introduce an Application read model/port that can return party-scoped financial truth with explicit data-completeness semantics. Its implementation must aggregate authoritative receivable/payable state without bypassing the later governed AR/AP lifecycle.

Only after that read model is defined should the field-level HTTP account-summary DTO be locked.

## 7. Proposed bounded Wave 2 sequence

The readiness evidence supports this sequence:

1. `W2.1` — `API-SAL-009 getSaleFiscalizationStatus`
   - field-level contract lock;
   - bounded read-only implementation;
   - no schema change expected from current evidence.
2. `W2.2` — `API-SAL-008 cancelSale`
   - lifecycle/irreversible-boundary contract lock;
   - Domain + Application + WebApi + persistence compatibility and durable evidence.
3. `W2.3` — `API-POS-001 getPosBootstrap`
   - bootstrap composition/freshness contract lock;
   - compose authoritative readers only.
4. `W2.4` — `API-PTY-008 getPartyAccountSummary`
   - party-scoped AR/AP read-model prerequisite;
   - field-level balance/aging contract lock;
   - public read endpoint after authoritative financial projection exists.
5. `W2.5` — Wave 2 reconciliation and runtime closure.

This ordering prioritizes the operation with the strongest existing domain/persistence foundation and prevents larger policy/read-model decisions from contaminating the first Wave 2 increment.

## 8. QA expectations for every Wave 2 increment

Each implementation increment must preserve the existing governance model:

- exact method/path/operationId/permission tests;
- 401/403 and organization-isolation behavior;
- RFC 9457 Problem Details semantics;
- idempotency and optimistic concurrency tests where the operation mutates state;
- architecture dependency-direction tests;
- provider-real PostgreSQL/MySQL tests when persistence behavior changes;
- regression coverage for the already-implemented Wave 2 surfaces;
- exact-head Clean Architecture Guard before merge;
- production migration/deployment/runtime gates remain separate where applicable.

No test or invariant may be weakened merely to obtain green CI.

## 9. Next governed decision

No product code should be changed by this audit.

The next proposed owner decision is to approve the field-level contract for `W2.1 / API-SAL-009 getSaleFiscalizationStatus`. Once that contract is locked, implementation can proceed as a bounded read-only slice against the current fiscalization request/document persistence.
