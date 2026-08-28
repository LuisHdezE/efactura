# Offline, Idempotency, Inbox and Outbox Architecture

## Failure modes are distinct

1. `CLIENT_ONLINE + API_ONLINE + FISCAL_TRANSPORT_ONLINE`
2. `CLIENT_ONLINE + API_ONLINE + DGI/PROVIDER_UNAVAILABLE`
3. `CLIENT_OFFLINE_OR_API_UNREACHABLE`

The architecture never collapses these into one `offline` boolean.

## Command identity

Every retry-sensitive command has a stable operation identity.

Online HTTP commands use `Idempotency-Key` where required.

Offline clients additionally send:

- `clientOperationId`;
- `deviceId`;
- `clientSequence`;
- `occurredAt`;
- dependency operation IDs when applicable;
- payload/body;
- local authorization/grant metadata required by the sync contract.

## Idempotency record

Logical fields:

- scope/company/actor/device;
- operation/command name;
- key/clientOperationId;
- normalized material payload hash;
- state: `IN_PROGRESS/APPLIED/REJECTED/CONFLICT`;
- canonical result/status/reference;
- created/completed timestamps;
- retention metadata.

Rules:
- same identity + same material payload returns prior canonical result;
- same identity + different material payload returns conflict and audit event;
- a crash after local commit but before HTTP response still resolves to one business effect;
- fiscal/financial idempotency evidence is not discarded on a short cache TTL merely for convenience.

## Inbox

External/provider callbacks/messages enter through an inbox/deduplication boundary before changing business state.

Inbox stores message/provider identity, hash, received time and handling state. Replayed provider messages cannot apply acknowledgement/payment/fiscal state twice.

## Outbox

Local transaction persists business change plus outbox intent atomically. A background worker dispatches after commit.

Outbox use cases:

- DGI/provider transport;
- asynchronous acknowledgements/work items;
- email/document delivery;
- reporting projections;
- integration events.

Worker processing is retry-safe and observable. External HTTP call is never inside the source transaction.

## Offline synchronization

Suggested API contract family for later API-design phase:

- `POST /api/v1/sync/batches`
- `GET /api/v1/sync/batches/{batchId}`
- `GET /api/v1/sync/changes?cursor=...`
- `GET /api/v1/sync/operations/{clientOperationId}`

A batch is not a giant all-or-nothing transaction. Each operation returns deterministic status:

- `APPLIED`
- `ALREADY_APPLIED`
- `REJECTED`
- `CONFLICT`
- `REVIEW_REQUIRED`
- `DEPENDENCY_BLOCKED`

Client resumes by cursor/operation identity.

## Conflict policy

Server is authoritative for fiscal, permission, financial and stock invariants. Offline timestamp does not automatically win.

Examples:
- duplicated sale confirmation -> prior result;
- stock no longer available -> conflict/review according to approved stock policy;
- permission revoked -> reject/review;
- fiscal rule changed -> revalidate against rule applicable to operation/fiscal date and approved policy;
- reused operation ID with changed amount -> hard conflict + audit.

## CFC contingency

Offline business queuing and formal fiscal contingency are separate.

Offline client must not allocate arbitrary normal CFE/CAE numbers. If the business must legally complete documentation while the electronic issuance system cannot operate, the formal CFC workflow is used and its original fiscal identity is preserved through synchronization/recovery.

## Device registration

A device record supports:
- company/location association;
- device status/revocation;
- last sync/cursor;
- offline policy/grant metadata;
- audit context.

Device identity is not a substitute for user authorization.
