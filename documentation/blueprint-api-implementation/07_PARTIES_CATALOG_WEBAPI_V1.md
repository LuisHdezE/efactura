# Parties and Catalog WebApi v1 Implementation

Status: `IMPLEMENTED_PENDING_REVIEW` on branch `blueprint/parties-catalog-webapi-v1`.

This slice completes the first executable business vertical over the accepted Clean Architecture and transactional persistence foundations. It does not claim the entire API implementation gate.

## Implemented contract operations

### Parties

- `API-PTY-001` `listParties` — GET `/api/v1/parties`;
- `API-PTY-002` `createParty` — POST `/api/v1/parties`;
- `API-PTY-003` `getParty` — GET `/api/v1/parties/{partyId}`;
- `API-PTY-004` `updateParty` — PATCH `/api/v1/parties/{partyId}`;
- `API-PTY-005` `addPartyFiscalIdentity` — POST `/api/v1/parties/{partyId}/fiscal-identities`;
- `API-PTY-006` `updatePartyFiscalIdentity` — PUT `/api/v1/parties/{partyId}/fiscal-identities/{identityId}`;
- `API-PTY-007` `setPartyRoles` — PUT `/api/v1/parties/{partyId}/roles`.

`API-PTY-008 getPartyAccountSummary` remains deferred until Receivables/Payables projections exist. It is not faked from Party master data.

### Catalog

- `API-CAT-001` `listItems` — GET `/api/v1/items`;
- `API-CAT-002` `createItem` — POST `/api/v1/items`;
- `API-CAT-003` `getItem` — GET `/api/v1/items/{itemId}`;
- `API-CAT-004` `updateItem` — PATCH `/api/v1/items/{itemId}`;
- `API-CAT-005` `deactivateItem` — POST `/api/v1/items/{itemId}/deactivate`;
- `API-CAT-006` `listItemCategories` — GET `/api/v1/item-categories`;
- `API-CAT-007` `createItemCategory` — POST `/api/v1/item-categories`;
- `API-CAT-008` `updateItemCategory` — PATCH `/api/v1/item-categories/{categoryId}`.

There is deliberately no public `GET /api/v1/item-categories/{categoryId}` because it is not part of the accepted API inventory. Application can query a category internally to construct responses and validate item assignment without expanding the public contract.

`API-CAT-009 listTaxProfiles` remains owned by the future Taxation slice.

## Clean Architecture conformance

- public HTTP DTOs live in WebApi and are mapped explicitly;
- controllers depend on Application use cases/contracts, never EF Core, DbContext, Dapper, provider types or legacy `ApplicationCore` services;
- Domain owns Party, fiscal-identity, CommercialItem and ItemCategory invariants;
- Application owns authorization/scope checks, idempotency behavior, workflow coordination, audit/outbox intent and repository ports;
- Infrastructure implements repositories with EF Core;
- repositories stage changes only and never call `SaveChanges`, `Commit` or `Rollback`;
- `IUnitOfWork` owns relational flush and `ITransactionManager` owns the local transaction boundary.

Architecture tests guard these rules automatically.

## Transaction rule

Every mutable operation follows the same authoritative local pattern:

1. authorize actor permission and company scope;
2. reserve `Idempotency-Key` using a server-computed material request hash;
3. flush the reservation inside the open transaction;
4. load/validate and mutate the aggregate;
5. stage the business row changes;
6. append durable audit evidence;
7. enqueue the integration event in Outbox;
8. complete the idempotency record;
9. `SaveChanges`;
10. commit.

Any exception/cancellation before commit rolls back business state, audit, Outbox and idempotency together. No mutable v1 repository uses flat SQL or Dapper.

## Organization context

The effective company is never trusted from a request body.

- an authenticated actor with exactly one company scope gets that company inferred;
- an actor with multiple allowed company scopes must send `X-Organization-Id`;
- the header is selection context only and is rejected when outside actor scopes;
- Application checks company scope again before performing reads or mutations.

## Concurrency

Party, CommercialItem and ItemCategory `Version` properties are EF Core concurrency tokens. Mutable requests carry `expectedVersion` and stale changes return the standardized `409 concurrency_conflict` path. Infrastructure also translates `DbUpdateConcurrencyException` into the same safe Application error contract.

## Idempotency

All mutable Parties/Catalog routes require `Idempotency-Key`. The WebApi computes the SHA-256 material request hash from the accepted DTO rather than accepting a client-provided hash.

Same key + same request returns the canonical existing resource without duplicating business, audit or Outbox rows. Same key + changed request returns conflict.

## National/foreign receiver model

Party continues to model these as independent facts:

- residence country;
- tax-residence country;
- fiscal identity type/number;
- fiscal identity issuing country;
- identity validity.

There is no authoritative `IsForeign` switch. This slice stores Party facts only; fiscal/tax classification remains a later Taxation/Fiscal decision.

## Deliberate contract gaps

### Addresses and contacts

The accepted target says Party will eventually carry addresses/contacts, but the exact field-level API contract and normalization rules have not yet been accepted. This slice therefore does not invent country/address/contact DTO structures and does not claim FR-010 fully implemented. Those fields require a reviewed contract amendment before implementation.

### Tax profile assignment

Tax-profile selection is not yet authoritative because the Taxation slice and its versioned rule catalog are not implemented. `taxProfileId` remains visible in the broader target DTO concept for future compatibility, but this implementation rejects any non-null assignment with `catalog.tax_profile_assignment_pending` rather than persisting an arbitrary unvalidated ID.

Existing items created in this slice therefore have no active TaxProfileId until Taxation is implemented and approved.

### Category clearing

Category assignment can be validated and changed to another active category. Explicit clearing semantics are not claimed in this slice and require a small request-contract refinement before being exposed.

## Database provider composition

New v1 persistence is selected by deployment configuration:

- `V1Persistence:Provider = PostgreSql | MySql`;
- `V1Persistence:ConnectionStringName = <configured connection-string name>`.

If no v1 connection-string name is specified, the current Brownfield PostgreSQL connection-string name is used as compatibility fallback. Missing configured connection data causes startup failure rather than silently disabling v1 persistence.

No secret or concrete connection value is committed by this implementation.

The new v1 persistence model/migrations run on PostgreSQL and MySQL. The legacy application remains wired to its historical PostgreSQL `DBContext`; therefore this slice does not claim that the entire Brownfield application can yet run MySQL-only. Provider portability is proven for the new v1 persistence boundary and will expand as legacy verticals are migrated.

## Migrations

Production startup does not call `EnsureCreated` or automatically migrate the database. Provider-compatible EF migrations are applied as an explicit deployment step. CI applies the migration stream to fresh PostgreSQL and MySQL databases before integration tests.

## Legacy compatibility

No legacy `/api/...` controller is removed or rewritten in this slice. New functionality is exposed only under `/api/v1/...` and the Brownfield API continues operating until its consumer usage/deprecation plan is proven.

## Remaining gate work

This slice must pass on its exact reviewed HEAD:

- solution build;
- Clean Architecture tests;
- API route/contract guard tests;
- cross-cutting API tests;
- PostgreSQL migrations + create/update/rollback/idempotency/concurrency scenarios;
- MySQL migrations + the same scenarios.

Passing this slice does not authorize Sales/POS/Fiscal implementation to bypass a later human review boundary.
