# W2.1 — Sale fiscalization status contract

Status: `CONTRACT_LOCKED_ON_MAIN / IMPLEMENTATION_NOT_STARTED`

Authority condition: this contract becomes binding only when the exact PR HEAD containing this file is merged into `main` with explicit owner approval. Before that merge it is a contract-lock candidate and does not authorize product implementation.

Baseline used to derive the contract: `main@32b6f466eafbbf4c0795249d73e397b2b1b44c5b`.

API ID: `API-SAL-009`

Operation ID: `getSaleFiscalizationStatus`

Public surface:

```text
GET /api/v1/sales/{saleId}/fiscalization
permission: sales.read
idempotency: NO
```

## 1. Purpose

Expose the **local fiscalization workflow state** for one sale without implying DGI receipt, transport, acceptance, rejection, observation or any other external fiscal-authority state that this route does not authoritatively own.

The current repository already persists the required local facts:

- the Sale itself;
- one sale-scoped `FiscalizationRequest` created atomically by sale confirmation;
- `FiscalizationRequestStatus.Pending` and `FiscalizationRequestStatus.IdentityCreated`;
- the optional linked `FiscalDocument` identity once local fiscal identity has been created.

No new aggregate or database schema is required for this read projection.

## 2. Authorization and isolation

The operation must preserve the existing Sales read boundary:

- authentication is required through the existing v1 API pipeline;
- permission: `sales.read`;
- organization is resolved from the existing organization context, never from the request body or query string;
- organization scope is enforced through the same Sales authorization semantics already used by `GetSaleUseCase`;
- a sale id that exists only in another organization must return the same not-found result as an unknown sale id;
- W2.1 does not introduce a new location-level read restriction that does not already exist on `getSale`.

Stable cross-organization/unknown-sale result:

```text
404
code: sales.not_found
```

## 3. Public response DTO

The locked transport projection is:

```text
SaleFiscalizationStatusDto
- saleId: string UUID
- saleStatus: string
- workflowStatus: string
- fiscalizationRequestId: string UUID | null
- fiscalizationVersion: integer | null
- requestedAtUtc: timestamp | null
- cfeFamilyCode: integer | null
- cfeFamily: string | null
- formatVersion: string | null
- fiscalDocument: SaleFiscalDocumentIdentityDto | null
- statusAuthority: string
```

Nested local fiscal identity:

```text
SaleFiscalDocumentIdentityDto
- id: string UUID
- cfeTypeCode: integer
- cfeType: string
- series: string
- number: integer
- fiscalDate: date
- identityCreatedAtUtc: timestamp
```

### Enum/string format

Enum names follow the existing v1 API convention: uppercase snake-case strings derived from the server enum value.

Examples:

- sale status: `DRAFT`, `VALIDATED`, `CONFIRMED`;
- fiscal workflow status: `NOT_REQUESTED`, `PENDING`, `IDENTITY_CREATED`;
- CFE family/type names use the same uppercase snake-case mapping already used by Sales fiscal-preview DTOs.

`cfeFamilyCode` and `cfeTypeCode` are the corresponding server-owned numeric enum values, preserving the existing Sales preview pattern of code + name.

## 4. Status authority marker

`statusAuthority` is a required fixed transport value:

```text
LOCAL_WORKFLOW_ONLY_NOT_DGI_ACCEPTANCE
```

This value is deliberately explicit. A client must not reinterpret any W2.1 workflow status as external DGI state.

W2.1 does **not** expose fields named or semantically equivalent to:

- DGI accepted;
- DGI rejected;
- DGI received;
- DGI pending;
- transport submitted;
- transport acknowledged;
- external provider status.

Existing raw DGI/CFE consultation evidence belongs to separate governed fiscal surfaces and is not projected by this endpoint.

## 5. Workflow mapping

The public `workflowStatus` vocabulary is exactly:

```text
NOT_REQUESTED
PENDING
IDENTITY_CREATED
```

`NOT_REQUESTED` is a **read-model projection value**, not a new `FiscalizationRequestStatus` domain member.

### 5.1 NOT_REQUESTED

Return `200` with `workflowStatus = NOT_REQUESTED` only when:

- the Sale exists in the resolved organization;
- no sale-scoped `FiscalizationRequest` exists;
- the current Sale status is `DRAFT` or `VALIDATED`.

The nullable fiscalization fields must all be null:

- `fiscalizationRequestId`;
- `fiscalizationVersion`;
- `requestedAtUtc`;
- `cfeFamilyCode`;
- `cfeFamily`;
- `formatVersion`;
- `fiscalDocument`.

### 5.2 PENDING

Return `200` with `workflowStatus = PENDING` only when:

- the Sale is `CONFIRMED`;
- a sale-scoped `FiscalizationRequest` exists;
- the request status is `Pending`;
- the request carries no fiscal-document id or identity-created timestamp;
- no `FiscalDocument` already exists for that fiscalization request.

Projected request fields:

- request id;
- request version;
- requested timestamp;
- CFE family code/name;
- format version.

`fiscalDocument` must be null.

`PENDING` means only that the **local fiscalization work item exists and local fiscal identity has not yet been created**. It does not describe DGI state.

### 5.3 IDENTITY_CREATED

Return `200` with `workflowStatus = IDENTITY_CREATED` only when:

- the Sale is `CONFIRMED`;
- a sale-scoped `FiscalizationRequest` exists;
- request status is `IdentityCreated`;
- the request carries a non-empty `FiscalDocumentId` and `IdentityCreatedAtUtc`;
- a `FiscalDocument` exists for that request in the same organization;
- the document id matches the request `FiscalDocumentId`;
- the document `SaleId` matches the requested Sale.

Projected request fields remain populated and `fiscalDocument` contains only the bounded local identity fields defined above.

`IDENTITY_CREATED` means only that the immutable local fiscal-document identity has been created. It does not imply XML generation, signing, transport or DGI acceptance.

## 6. Fail-closed consistency rules

The endpoint must never normalize impossible persistence combinations into an apparently valid public status.

The following conditions return conflict:

```text
409
code: fiscalization.inconsistent_state
conflictType: inconsistent_state
```

At minimum this includes:

- Sale is `CONFIRMED` but no fiscalization request exists;
- a fiscalization request exists for a Sale that is not `CONFIRMED`;
- request is `Pending` but a FiscalDocument already exists for the request;
- request is `IdentityCreated` but the referenced FiscalDocument cannot be resolved;
- request/document ids disagree;
- resolved FiscalDocument points at a different Sale;
- a future/unsupported fiscalization workflow state reaches this endpoint before this contract is extended.

This follows the existing atomic confirmation invariant: a successfully confirmed Sale and its durable fiscalization request are committed together.

## 7. Application read model

Implementation should introduce a bounded read use case, conceptually:

```text
GetSaleFiscalizationStatusUseCase
```

Expected dependencies are existing read-capable repositories only:

- `ISaleRepository`;
- `IFiscalizationRequestRepository`;
- `IFiscalDocumentRepository`;
- `IActorContextAccessor` for the existing `sales.read` + organization-scope guard.

The use case must not:

- start a business transaction;
- call UnitOfWork/SaveChanges;
- reserve idempotency keys;
- append audit events merely for the read;
- enqueue outbox events;
- mutate Sale, FiscalizationRequest or FiscalDocument;
- call DGI/provider transport or consultation gateways.

## 8. HTTP behavior

Successful reads return `200`.

The endpoint has no request body and does not require `Idempotency-Key`.

Expected error boundaries:

- unauthenticated: existing global `401` semantics;
- authenticated without `sales.read`: existing `403 permission_denied` semantics;
- actor outside resolved organization scope: existing `403 organization_scope_denied` semantics;
- unknown/cross-organization sale id: `404 sales.not_found`;
- inconsistent local fiscalization state: `409 fiscalization.inconsistent_state` with `conflictType = inconsistent_state`.

Problem responses remain RFC 9457 `application/problem+json` through the existing v1 error pipeline.

## 9. Schema and deployment impact

Expected implementation impact from current evidence:

- Domain: no change required;
- database schema: no change expected;
- migration: none expected;
- Infrastructure: existing repository methods are sufficient unless implementation reveals a strictly read-only adapter gap;
- Application: one read use case/projection;
- WebApi: one GET action + DTO mapping + DI registration as required;
- OpenAPI: add exactly `GET /api/v1/sales/{saleId}/fiscalization` as `getSaleFiscalizationStatus`;
- no DELETE/POST/PATCH companion route is introduced by W2.1.

## 10. Required automated evidence for implementation

The later implementation PR must prove at minimum:

1. exact method/path/operationId/permission contract;
2. no JWT -> `401`;
3. malformed JWT -> `401` through existing pipeline;
4. missing `sales.read` -> `403`;
5. organization-scope escape -> `403 organization_scope_denied`;
6. unknown sale -> `404 sales.not_found`;
7. cross-organization sale id -> masked `404 sales.not_found`;
8. Draft without request -> `NOT_REQUESTED`;
9. Validated without request -> `NOT_REQUESTED`;
10. Confirmed + Pending request -> `PENDING` with request fields and null fiscal document;
11. Confirmed + IdentityCreated request + matching FiscalDocument -> `IDENTITY_CREATED` with bounded identity fields;
12. Confirmed without request -> `409 fiscalization.inconsistent_state`;
13. non-confirmed Sale with request -> `409 fiscalization.inconsistent_state`;
14. Pending request with preexisting FiscalDocument -> `409 fiscalization.inconsistent_state`;
15. IdentityCreated with missing/mismatched FiscalDocument -> `409 fiscalization.inconsistent_state`;
16. response always carries `statusAuthority = LOCAL_WORKFLOW_ONLY_NOT_DGI_ACCEPTANCE`;
17. no response field claims DGI/provider acceptance or transport state;
18. existing Wave 2 Sales regression endpoints remain green;
19. Clean Architecture Guard remains green;
20. provider-real PostgreSQL/MySQL must remain regression-green even though W2.1 is expected to add no persistence mutation.

## 11. Runtime acceptance boundary

W2.1 runtime acceptance is read-only with respect to the endpoint itself.

Any future runtime fixture creation is a separate protected operation and is not authorized by this contract. Production schema writes are not expected for W2.1.

## 12. Explicit non-scope

This contract does not implement or authorize:

- sale cancellation;
- CFE XML generation;
- XML signature;
- fiscal artifact transport;
- DGI/provider status interpretation;
- DGI accepted/rejected lifecycle projection;
- raw external fiscal consultation evidence on this route;
- correction notes;
- contingency workflow;
- fiscal representation download;
- any production mutation.

## 13. Effect of owner-approved merge

Merging the exact PR HEAD containing this document into `main` constitutes the W2.1 **field-level contract lock only**.

After that merge:

- `API-SAL-009` may move from `IMPLEMENTATION_READY_PENDING_CONTRACT_LOCK` to `CONTRACT_LOCKED_IMPLEMENTATION_READY`;
- product implementation still requires a separate implementation branch/PR;
- implementation merge remains separately owner-gated;
- deployment/runtime/prod-write gates remain separate where applicable.
