# W2.3 POS bootstrap contract

Status: `CONTRACT_CANDIDATE_PENDING_OWNER_LOCK / IMPLEMENTATION_NOT_AUTHORIZED`

API ID: `API-POS-001`

Operation ID: `getPosBootstrap`

Accepted public surface:

```text
GET /api/v1/pos/bootstrap
permission: sales.read
idempotency: NO
```

Parent readiness audit: `W2_READINESS_AUDIT.md`.

This document resolves the prerequisite identified by the Wave 2 readiness audit. It is a contract candidate only. No product implementation is authorized until an owner-approved merge locks this contract.

## 1. Governing boundary

`getPosBootstrap` is a compact, read-only operational-context bootstrap for the POS client.

Its purpose is to provide the authorized active location/terminal combinations that a sales operator may select before creating a Sale.

It is **not** a general aggregation endpoint and MUST NOT absorb unrelated dependencies merely because they are visible in the POS UI.

In particular W2.3 MUST NOT embed:

- payment methods;
- commercial-item/catalog results;
- party/customer results;
- price lists, promotions or discounts;
- tax-profile details;
- CFE/fiscal-state information;
- cash-shift state;
- stock balances;
- offline/sync configuration;
- secrets, provider configuration or infrastructure metadata.

Those remain owned by their existing or separately governed APIs.

`API-PMT-001 listPaymentMethods` remains an independent dependency. W2.3 MUST NOT fabricate or duplicate payment-method authority inside the bootstrap response.

## 2. Authorization contract

The endpoint requires exactly:

```text
sales.read
```

The resolved organization must remain inside the current actor company scope using the existing sales authorization semantics.

W2.3 MUST NOT silently require `organization.read` in addition to `sales.read`.

This is significant because the existing organization read use cases (`ListFiscalLocationsUseCase` and `ListTerminalsUseCase`) enforce `organization.read`. The W2.3 implementation therefore requires a dedicated POS bootstrap read use case/read model rather than composing those public organization use cases directly.

The implementation MAY reuse their inward repository ports and persisted records, but the authorization boundary belongs to `API-POS-001` and remains `sales.read`.

Global authentication behavior remains unchanged:

- unauthenticated -> existing `401` Problem Details behavior;
- authenticated without `sales.read` -> `403 permission_denied`;
- requested organization outside actor company scope -> `403 organization_scope_denied`.

## 3. Operational scope filtering

Only active operational context that the actor may actually use is returned.

### Locations

A location is returned only when all of the following are true:

1. it belongs to the resolved organization;
2. it is active;
3. its id is present in the actor `LocationScopes` set.

An actor with no location scope receives an empty location/context set rather than an unscoped organization-wide location projection.

This intentionally prevents the bootstrap from presenting location choices that later sales operations would reject at the application boundary.

### Terminals

A terminal is returned only when all of the following are true:

1. it belongs to the resolved organization;
2. it is active;
3. its `LocationId` belongs to a returned active location;
4. when the actor has one or more `TerminalScopes`, its id is included in that set.

When the actor has no explicit terminal scopes, active terminals under an allowed location are not further restricted by terminal id. This preserves the current terminal-scope semantics already used by sales operations.

No cross-organization location or terminal may appear in the response.

## 4. Response shape

Successful bootstrap returns HTTP `200` with a compact response shaped as follows:

```json
{
  "organizationId": "org-1",
  "generatedAtUtc": "2026-09-22T02:00:00Z",
  "contexts": [
    {
      "locationId": "location-1",
      "locationName": "Casa Central",
      "dgiBranchCode": "0001",
      "locationVersion": 3,
      "terminals": [
        {
          "terminalId": "terminal-1",
          "code": "POS-01",
          "name": "Caja 1",
          "terminalVersion": 4
        }
      ]
    }
  ]
}
```

The exact DTO names are implementation detail, but the field-level public response contract is locked by this section.

### Top-level fields

- `organizationId`: resolved organization id;
- `generatedAtUtc`: server UTC timestamp at which the authorized bootstrap projection was assembled;
- `contexts`: active, authorized location/terminal combinations.

### Location context fields

- `locationId`;
- `locationName`;
- `dgiBranchCode`;
- `locationVersion`;
- `terminals`.

### Terminal fields

- `terminalId`;
- `code`;
- `name`;
- `terminalVersion`.

Only fields required to identify and safely select operational context are exposed. Fiscal address, city, department and unrelated organization metadata are deliberately excluded from the compact bootstrap.

## 5. Selection semantics

The server returns allowed choices; it does not invent a selected/default terminal.

W2.3 introduces no implicit device-to-terminal binding and no server-owned auto-selection policy because no authoritative mapping currently exists between `ActorContext.DeviceId` and a Terminal.

Therefore:

- the response does not contain `defaultLocationId`;
- the response does not contain `defaultTerminalId`;
- a client must explicitly choose a valid location/terminal pair unless it has a separately governed local preference that still matches the latest bootstrap projection;
- a previously cached client selection must be discarded when it is no longer present in the current bootstrap response.

A future explicit device/terminal assignment policy may extend this contract without redefining the current authorization boundary.

## 6. Empty-state behavior

The endpoint returns HTTP `200` with an empty `contexts` collection when the actor has no currently usable active POS context.

Examples:

- no actor location scopes;
- every scoped location is inactive;
- allowed active locations exist but no active terminal remains under them after terminal-scope filtering.

This is not a `404` and not a permission error when the actor is otherwise authenticated, has `sales.read`, and is inside the requested organization scope.

The client must treat an empty bootstrap as an operational-configuration state and must not invent location or terminal identifiers.

## 7. Freshness and cache contract

The endpoint is a cache-friendly read projection, but stale authorization or operational context must never become silently authoritative.

Required HTTP behavior:

```text
Cache-Control: private, no-cache
ETag: <opaque projection tag>
```

Semantics:

- `private` prevents shared/intermediary caches from reusing an actor-scoped bootstrap across users;
- `no-cache` permits private storage but requires revalidation before reuse;
- the ETag is opaque to clients;
- the ETag must change whenever the authorized response projection changes;
- the ETag input must include the resolved organization, effective actor location/terminal scope projection, and the returned active location/terminal identity+version data;
- the ETag MUST NOT expose raw actor identifiers, permissions, scope identifiers beyond values already present in the authorized body, secrets or provider data.

When `If-None-Match` matches the current authorized projection, the endpoint returns HTTP `304` with no body.

`generatedAtUtc` is freshness evidence for a `200` projection. It is not part of the semantic ETag payload and therefore must not force a different ETag on every request when the underlying authorized data is unchanged.

W2.3 introduces no arbitrary fixed TTL such as 30/60 seconds. Revalidation is the freshness authority for v1.

## 8. Relationship to organization APIs

Existing APIs remain independently valid:

```text
GET /api/v1/locations   -> organization.read
GET /api/v1/terminals   -> organization.read
```

`getPosBootstrap` does not replace these administrative/organization reads.

The distinction is intentional:

- organization APIs expose organization configuration under `organization.read`;
- POS bootstrap exposes only the minimal sales-operational choices required by an actor with `sales.read`.

W2.3 must not weaken or broaden the permissions of the existing organization endpoints.

## 9. Relationship to Sale creation

`SaleCreateRequest` already accepts `LocationId` and `TerminalId` as optional public fields.

The POS UI, however, requires a valid operational context for the governed POS flow. `getPosBootstrap` provides the authorized source for those choices without changing the Sale command contract.

W2.3 does not modify:

- `createSale`;
- `updateSaleDraft`;
- sale-domain lifecycle;
- confirmation/cancellation semantics;
- fiscalization behavior.

No Sale is created, updated, validated, confirmed or cancelled by the bootstrap endpoint.

## 10. Payment-method boundary

Payment methods are deliberately excluded.

The accepted inventory already defines:

```text
API-PMT-001 listPaymentMethods
GET /api/v1/payment-methods
permission: payments.read
```

That API has a distinct permission and lifecycle authority. Folding payment methods into `getPosBootstrap` would either bypass `payments.read` or force W2.3 to require an undeclared second permission.

Both outcomes violate the accepted API inventory.

Therefore the POS client must load payment methods through `API-PMT-001` when that endpoint is implemented. Until then, W2.3 alone does not make settlement-method selection live.

## 11. Catalog, parties and pricing boundary

The bootstrap does not duplicate:

```text
API-CAT-001 listItems
API-PTY-001 listParties
```

Those surfaces already own catalog/customer searching and carry their own permissions.

The current catalog also does not expose an authoritative selling price. W2.3 must not invent prices, price lists, discounts or promotions.

The POS continues to capture `UnitPrice` through the Sale line contract until a separately governed pricing authority exists.

## 12. Read-only application boundary

The W2.3 use case is pure read orchestration.

It MUST NOT use:

- transaction manager;
- Unit of Work / `SaveChanges`;
- idempotency store;
- audit writer;
- outbox writer;
- payment repositories;
- party repositories;
- catalog repositories;
- fiscal/DGI gateways;
- stock mutation services.

It may depend on:

- `IFiscalLocationRepository`;
- `ITerminalRepository`;
- `IActorContextAccessor`;
- a clock abstraction if one is already accepted for `generatedAtUtc`, otherwise the implementation may use the project-standard server UTC source;
- a small pure projection-tag/ETag helper owned by WebApi or Application without persistence side effects.

No schema change or migration is expected.

## 13. Consistency rules

The bootstrap only returns terminal/location pairs that are internally usable as an operational choice.

An active terminal referencing a location that is missing, inactive, outside the resolved organization or outside the actor location scope is omitted from the projection.

This read endpoint does not repair inconsistent records and does not mutate operational configuration.

The existing organization mutation workflows remain authoritative for correcting such configuration.

## 14. Stable ordering

To keep the response deterministic and to make ETag computation stable, the public projection order is locked:

1. contexts ordered by `locationName`, then `locationId`, using ordinal/case-insensitive display ordering with `locationId` as deterministic tie-breaker;
2. terminals within a context ordered by `code`, then `terminalId`, with `terminalId` as deterministic tie-breaker.

The exact database query order may differ, but the public response and ETag input must use deterministic normalized ordering.

## 15. Required implementation evidence

Before implementation merge approval, automated evidence must cover at least:

### Contract / architecture

- exact GET route `/api/v1/pos/bootstrap`;
- operationId `getPosBootstrap`;
- exact public permission `sales.read`;
- no idempotency requirement;
- exact DTO field set from this contract;
- no dependency on payment/catalog/party/fiscal/stock mutation surfaces;
- no transaction/UoW/audit/outbox/idempotency mutation dependencies.

### Authorization / isolation

- unauthenticated -> existing `401` behavior;
- missing `sales.read` -> `403 permission_denied`;
- company-scope escape -> `403 organization_scope_denied`;
- no requirement for `organization.read`;
- no cross-organization location/terminal leakage.

### Scope filtering

- only active locations are returned;
- only locations inside actor `LocationScopes` are returned;
- empty location scope -> HTTP 200 with empty contexts;
- only active terminals under returned locations are returned;
- non-empty actor `TerminalScopes` restrict terminal ids;
- empty actor `TerminalScopes` does not add a terminal-id restriction beyond the allowed location set;
- inactive/mismatched terminal records are omitted.

### Projection

- no default location/terminal is fabricated;
- deterministic context/terminal ordering;
- empty operational state returns 200 + empty contexts;
- response does not contain payment methods, items, parties, prices, tax details or fiscal state.

### Freshness

- `Cache-Control: private, no-cache`;
- stable ETag for unchanged authorized projection;
- changed authorized data/scope changes ETag;
- matching `If-None-Match` -> 304 with no response body;
- `generatedAtUtc` present on 200 and not used to force ETag churn.

## 16. Persistence and production impact

Expected schema impact: **none**.

Expected production data mutation during ordinary runtime acceptance: **none**.

A W2.3 runtime acceptance can be read-only and may validate:

- Swagger/OpenAPI route exposure;
- 401 without JWT;
- 403 permission behavior;
- organization isolation;
- current empty/non-empty scoped bootstrap projection;
- ETag/304 behavior.

No location, terminal, Sale, payment method, item or party needs to be created solely to prove the read endpoint exists and enforces its boundary.

If implementation reveals a genuine missing persistence capability that would require schema changes, implementation MUST stop at a new schema gate rather than silently introduce a migration.

## 17. Explicit non-goals

W2.3 does not implement or authorize:

- `API-PMT-001 listPaymentMethods`;
- cash-shift/session bootstrap;
- terminal registration or activation;
- location creation/reactivation;
- device enrollment/binding;
- default terminal policy;
- item/customer preload;
- stock snapshot preload;
- pricing authority;
- fiscal/CAE state preload;
- offline-authoritative POS execution;
- background sync configuration.

## 18. Contract-lock effect

Owner-approved merge of this document will lock only the W2.3 field-level/public behavior contract.

It will authorize a later bounded implementation increment for `API-POS-001` under the normal exact-head CI, merge, deployment and runtime gates.

It does not authorize production mutation and it does not authorize implementation of `API-PMT-001` or any other Wave 3 dependency.
