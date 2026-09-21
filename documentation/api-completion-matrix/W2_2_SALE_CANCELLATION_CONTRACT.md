# W2.2 Sale cancellation contract

Status: `CONTRACT_CANDIDATE_PENDING_OWNER_LOCK / IMPLEMENTATION_NOT_AUTHORIZED`

API ID: `API-SAL-008`

Operation ID: `cancelSale`

Accepted public surface:

```text
POST /api/v1/sales/{saleId}/cancel
permission: sales.cancel
idempotency: REQUIRED
```

Parent readiness audit: `W2_READINESS_AUDIT.md`.

This document resolves the prerequisite identified by the Wave 2 readiness audit. It is a contract candidate only. No product implementation is authorized until an owner-approved merge locks this contract.

## 1. Governing boundary

Cancellation is a pre-confirmation terminal transition. It is not a reversal mechanism.

The current confirmation transaction is already an irreversible local boundary because confirmation atomically persists one or more of the following authoritative effects:

- Sale confirmation evidence;
- immediate Payment records;
- a Receivable when credit remains outstanding;
- tracked-stock consumption when applicable;
- a durable FiscalizationRequest;
- successful audit evidence;
- outbox integration events;
- idempotency completion.

Therefore `cancelSale` MUST NOT reverse, delete, compensate, void, credit-note, or otherwise mutate confirmed effects. Any later correction/reversal of a confirmed transaction belongs to separately governed fiscal/financial workflows.

## 2. Sale lifecycle

W2.2 extends the domain status vocabulary with:

```text
Draft = 1
Validated = 2
Confirmed = 3
Cancelled = 4
```

Allowed transitions through `cancelSale`:

```text
DRAFT     -> CANCELLED
VALIDATED -> CANCELLED
```

Forbidden transitions:

```text
CONFIRMED -> CANCELLED
CANCELLED -> CANCELLED   (except idempotent replay of the same completed request)
```

`CONFIRMED` is the hard irreversible boundary for this endpoint.

`CANCELLED` is terminal for the ordinary Sale lifecycle:

- it remains queryable through existing Sale read surfaces;
- it may appear in existing list/filter projections as `CANCELLED`;
- it cannot be edited, validated, confirmed, or cancelled again by a new command;
- no implicit re-open/reactivate transition is introduced by W2.2.

## 3. Historical evidence

Cancellation MUST preserve history rather than rewrite it.

For a previously `VALIDATED` Sale:

- `ValidationFingerprint` remains unchanged;
- `ValidatedAtUtc` remains unchanged.

For a `DRAFT` Sale those fields remain null as they already were.

Because only pre-confirmation states are cancellable:

- `ConfirmationFingerprint` MUST remain null;
- `SettlementFingerprint` MUST remain null;
- `ConfirmedAtUtc` MUST remain null.

Cancellation increments `Sale.Version` exactly once on the first successful transition.

No cancellation timestamp or reason column is added to the Sale record in W2.2. Durable cancellation evidence is owned by audit/outbox, while Sale persists the terminal state and optimistic-concurrency version.

## 4. Request contract

Request body:

```json
{
  "expectedVersion": 2,
  "operatorReason": "Customer requested cancellation before confirmation",
  "operatorContext": "optional free-form operational context"
}
```

Fields:

- `expectedVersion`: required positive integer;
- `operatorReason`: required non-empty string, trimmed, maximum 500 characters;
- `operatorContext`: optional string, trimmed when present, maximum 1000 characters.

W2.2 does not introduce a mandatory cancellation reason code. No authoritative reason-code taxonomy exists in the accepted contract or current domain, so inventing one would create a second unsupported policy source. A governed taxonomy may be introduced later without weakening this audit trail.

The HTTP request MUST also require `Idempotency-Key` using the existing v1 request contract.

## 5. Success response

Successful cancellation returns HTTP `200` with:

```json
{
  "saleId": "uuid",
  "version": 3,
  "status": "CANCELLED",
  "replayed": false
}
```

Response fields:

- `saleId`: Sale identifier;
- `version`: current Sale version after cancellation;
- `status`: always `CANCELLED` for success;
- `replayed`: `true` only when the same completed idempotent request is replayed.

The response deliberately excludes cancellation timestamp/reason fields so replay does not require a new Sale persistence snapshot. Full operational reason/context remains available through audit evidence, not duplicated into the Sale aggregate.

## 6. Authorization and organization isolation

The endpoint requires `sales.cancel`.

Organization isolation follows existing Sales semantics:

- unknown Sale in the resolved organization -> `404 sales.not_found`;
- a Sale belonging to another organization MUST remain masked as `404 sales.not_found`;
- actor organization/company scope escape remains denied by the existing authorization boundary.

No existence leak is permitted.

## 7. Optimistic concurrency

`expectedVersion` is mandatory.

If the current Sale version differs from `expectedVersion`, return:

```text
409
code: concurrency.stale_version
conflictType: stale_version
currentVersion: <current Sale version>
```

The stale-version check applies before the first state transition. A completed replay of the exact same idempotent request is resolved by idempotency replay semantics instead of failing stale-version merely because the successful first execution already incremented the version.

## 8. Irreversible-boundary and invalid-state conflicts

For a newly acquired cancellation command, conflict precedence is locked as follows after Sale lookup and organization masking:

1. `CONFIRMED` -> irreversible-boundary conflict;
2. `CANCELLED` -> already-cancelled invalid-state conflict;
3. otherwise (`DRAFT` or `VALIDATED`), compare `expectedVersion` and return stale-version when it differs;
4. only then execute the allowed transition.

This precedence is intentional. A confirmed Sale must never appear potentially cancellable merely because a caller supplies a different version, and a terminal cancelled Sale must not invite another command by returning only a concurrency hint. Completed idempotent replay remains resolved before these new-command state checks.

### Confirmed Sale

A confirmed Sale MUST fail closed:

```text
409
code: sales.cancellation.irreversible_boundary_crossed
conflictType: irreversible_boundary
currentVersion: <current Sale version>
```

This endpoint MUST NOT attempt to remove Payment, Receivable, stock, FiscalizationRequest, fiscal, audit, outbox or idempotency evidence created by confirmation.

### Already cancelled with a different/new command

A new cancellation command against an already cancelled Sale returns:

```text
409
code: sales.already_cancelled
conflictType: invalid_state
currentVersion: <current Sale version>
```

The only non-conflicting repeat is a completed replay under the same idempotency scope/key/hash.

## 9. Idempotency contract

Idempotency scope:

```text
sales.cancel:{organizationId}:{saleId}
```

The existing `Idempotency-Key` and canonical request hash semantics apply.

Required behavior:

- first valid request acquires the reservation and executes once;
- same scope + same key + same payload after completion -> deterministic replay, HTTP 200, `replayed = true`, no new version increment and no duplicate durable evidence;
- same scope + same key + different payload -> `409 idempotency_key_reused`;
- reservation already in progress -> `409 idempotency_in_progress`.

Successful completion record result type:

```text
sale_cancelled
```

resource type:

```text
Sale
```

resource id: Sale id.

## 10. Atomic transaction boundary

The first successful cancellation is one local transaction containing:

```text
Sale state/version update
+ audit evidence
+ SaleCancelled outbox event
+ idempotency completion
```

All four effects succeed or roll back together.

W2.2 MUST NOT create or mutate:

- Payment;
- Receivable;
- stock movement/position quantities;
- FiscalizationRequest;
- FiscalDocument;
- CAE/fiscal-number state;
- DGI/provider state.

This is intentionally much narrower than Sale confirmation.

## 11. Durable audit evidence

Successful cancellation writes one audit event:

```text
SALE_CANCELLED
```

Required audit metadata:

- previous status (`Draft` or `Validated`);
- new status (`Cancelled`);
- `operatorReason`;
- optional `operatorContext`;
- resulting Sale version.

The audit event uses the current actor and correlation context and the Sale organization/location/terminal context already used by other Sale mutations.

Failed authorization, validation, stale-version, irreversible-boundary, invalid-state and idempotency-conflict attempts MUST NOT leave successful cancellation audit/outbox/idempotency residue.

## 12. Outbox evidence

Successful cancellation enqueues one integration event:

```text
SaleCancelledIntegrationEvent
```

Minimum event payload:

- event id;
- occurred-at UTC;
- Sale id;
- organization id;
- previous Sale status;
- resulting Sale version.

The outbox event must be created in the same transaction as the Sale state change and audit/idempotency completion.

## 13. Persistence impact

Expected schema impact: **none**.

Rationale:

- current Sale persistence stores `Status` as an integer;
- `Cancelled = 4` fits the existing persisted status field;
- current persistence already stores `Version` as the optimistic-concurrency token;
- cancellation reason/context and timestamp are durable through existing audit/outbox infrastructure;
- the success response does not require a new cancellation snapshot column.

Implementation must nevertheless include provider-real PostgreSQL/MySQL persistence tests proving `Cancelled` round-trips correctly and version/concurrency behavior remains provider-neutral.

If implementation reveals an actual provider constraint not visible in the current model, that is a new schema gate and MUST stop implementation rather than silently introduce a migration.

## 14. Required implementation evidence

Before implementation merge approval, automated evidence must cover at least:

### Domain

- Draft -> Cancelled succeeds;
- Validated -> Cancelled succeeds and preserves validation evidence;
- Confirmed cancellation is rejected;
- Cancelled is terminal;
- successful cancellation increments version once;
- stale expected version is rejected.

### Application

- `sales.cancel` authorization;
- organization isolation / cross-org masking;
- required reason and length bounds;
- idempotency acquisition/completion/replay/payload-mismatch/in-progress behavior;
- exact irreversible-boundary and already-cancelled conflict codes;
- conflict precedence for Confirmed, Cancelled and stale-version states;
- atomic audit + outbox + idempotency completion;
- no confirmation-effect repositories/gateways are used for reversal.

### WebApi / contract

- exact `POST /api/v1/sales/{saleId}/cancel` route;
- operationId `cancelSale`;
- permission `sales.cancel`;
- required `Idempotency-Key`;
- DTO fields and enum casing exactly as locked here;
- RFC 9457 Problem Details mapping for 401/403/404/409;
- no DELETE or alternative cancellation route.

### Provider-real

- PostgreSQL and MySQL round-trip of `SaleStatus.Cancelled`;
- version/concurrency token behavior;
- first success persists exactly one audit event, one outbox event and one completed idempotency record;
- replay introduces no duplicate durable evidence;
- confirmed/stale/validation failures leave no successful cancellation residue.

## 15. Runtime gate

Any production runtime acceptance that creates and cancels a Sale is a production-data mutation and requires separate explicit owner approval.

No production mutation is authorized by locking this contract or by implementing W2.2.

## 16. Contract lock effect

Owner-approved merge of this document locks only the W2.2 lifecycle/HTTP/durability contract.

It authorizes the next bounded implementation increment to modify Domain/Application/WebApi/tests and the existing Sale persistence adapter as required by this contract, subject to normal exact-head CI and merge approval.

It does not authorize:

- production data mutation;
- production schema writes;
- compensation/reversal of confirmed Sale effects;
- fiscal correction/credit-note behavior;
- implementation of W2.3 or W2.4.
