# W3.1 Payment methods — readiness audit and contract candidate

Status: `CONTRACT_CANDIDATE_PENDING_OWNER_LOCK / IMPLEMENTATION_NOT_AUTHORIZED`

Baseline: `main@a160fb961d426182b344b2517b79274e8bc84e84`.

Scope: `API-PMT-001 listPaymentMethods`, `API-PMT-002 createPaymentMethod`, and
`API-PMT-003 updatePaymentMethod`. All three remain `MISSING_HTTP`; Wave 3
remains `0/24` until their own merge, deploy, and runtime acceptance gates close.

## 1. Readiness evidence

| Layer | Current evidence | Missing for the three public operations |
|---|---|---|
| Domain | `EFactura.Domain.Payments.PaymentMethod`: organization, name, enabled, version; rename/enable transitions | No payment-medium classification (cash/card/bank); do not derive one from the name |
| Application | `IPaymentMethodRepository.GetAsync/AddAsync/SaveAsync`; sale confirmation loads enabled method evidence | List/search and three governed use cases with actor authorization, idempotency, audit, and outbox |
| Persistence | `v1_payment_methods` and `EfPaymentMethodRepository`; `(OrganizationId, Enabled)` index and version concurrency token | Organization-filtered listing; verify concurrent updates and no-op behavior against PostgreSQL and MySQL |
| HTTP | Legacy `api/[controller]` CRUD uses the Brownfield integer ID and PUT contract | All three `/api/v1/payment-methods` operations, UUID identities, permissions, DTOs and API QA |

The legacy controller and the sale-settlement reader do not satisfy these public
operations. Existing sale payments store method ID and version as historical
evidence; renaming or disabling a method must not rewrite those payments.

## 2. Public contract candidate

| API ID | operationId | HTTP | Permission | Idempotency |
|---|---|---|---|---|
| `API-PMT-001` | `listPaymentMethods` | GET `/api/v1/payment-methods` | `payments.read` | No |
| `API-PMT-002` | `createPaymentMethod` | POST `/api/v1/payment-methods` | `payments.manage` | Required |
| `API-PMT-003` | `updatePaymentMethod` | PATCH `/api/v1/payment-methods/{paymentMethodId}` | `payments.manage` | Required |

Organization is resolved from the authenticated actor and validated against
company scope. It is never trusted from the request payload. IDs are UUIDs
backed by the v1 aggregate, not Brownfield integer IDs. Missing or cross-scope
resources return the same `404 payments.method_not_found` without revealing
another organization's method.

### List

GET returns HTTP 200 and a bounded `PageResponse<PaymentMethodDto>` with
`items`, `page`, `pageSize`, and `total`. Default `enabled=true` exposes usable
methods for POS/treasury; `enabled=false` lists disabled methods to authorized
administrators who also hold `payments.read`. No unbounded list is permitted.
Sort deterministically by normalized display name, then ID. Empty results
return 200 with `items=[]` and `total=0`. This operation does not include
classification, settlement availability, custody, fees, or FX policy.

### Create

Request: `{ "name": "Transferencia" }`. The server assigns ID, organization,
`enabled=true`, and `version=1`. Name is trimmed, nonblank, at most 120
characters. Return HTTP 201 with the canonical DTO. The operation needs
`Idempotency-Key`, a durable `payment_method.created` audit event, and an
outbox record in the same local transaction. Exact replay returns the prior
resource without a second audit/outbox event and signals replay with
`Idempotent-Replayed: true`.

### Update

PATCH request: `{ "expectedVersion": 3, "name": "Tarjeta", "enabled": false }`.
At least one of `name` or `enabled` must be present; explicit JSON null is
invalid for either field. An omitted field is unchanged. `expectedVersion`
must match the current persisted version. A material update increments the
version exactly once even when both fields change. A valid no-op preserves
the version and creates no change audit/outbox event. Return HTTP 200 with
the canonical DTO. A material change emits `payment_method.updated` in the
same transaction as the mutation and idempotency completion. A disabled
method remains readable for administration and cannot be used for new sale
settlement; existing immutable sale payments remain intact.

The canonical DTO is `{ id, version, enabled, name }` with ID serialized as
UUID text. No organization ID is accepted in mutation payloads.

## 3. Error, retry and concurrency contract

- Unauthenticated: `401 authentication_required`.
- Missing permission: `403 permission_denied`.
- Organization outside actor scope: `403 organization_scope_denied`.
- Unknown/cross-organization method: `404 payments.method_not_found`.
- Invalid name, patch shape, or version: 400 Problem Details with stable
  payment-method validation codes to be fixed by implementation tests.
- Missing required idempotency key: `400 idempotency_key_missing`.
- Key reused with different material request: `409 idempotency_key_reused`.
- Stale `expectedVersion` or write race: `409 concurrency_conflict`,
  `conflictType=stale_version` and safe `currentVersion` when available.

Idempotency scope includes operation, organization and actor context. The
material request hash includes the path ID for PATCH, expected version, and
the presence and value of each patch field. A replay must recover the
canonical committed result after a response loss. No-op requests still
complete idempotency so retries have a stable result. The repository's
current `SaveAsync` assumes `newVersion=oldVersion+1`, so the use case must
skip it on a no-op and must use one combined domain transition for a patch
changing both name and enabled.

## 4. Prerequisites before implementation

1. Add an organization-filtered, bounded list port and provider-neutral
   implementation; do not use legacy `PaymentMethodService` or its tables.
2. Add one atomic aggregate update operation that validates both optional
   fields against one expected version, including explicit no-op semantics.
3. Build create/update workflows with actor/company scope, idempotency,
   audit and outbox inside the established transaction boundary.
4. Expose the three exact routes, permission attributes and DTOs; preserve
   the accepted route/operationId matrix and Brownfield compatibility.
5. Add contract/authorization/idempotency/concurrency tests and provider-real
   PostgreSQL/MySQL list and write tests. Verify historical sale payments and
   disabled-method rejection in new sale confirmation.
6. Run exact-head Clean Architecture Guard. Merge requires the owner's
   explicit exact-HEAD approval; deploy and runtime acceptance remain later
   gates. A production write or migration requires separate authorization.

## 5. Open decisions for owner lock

The list pagination upper bound and case-sensitive versus case-insensitive
name ordering should be fixed against the repository's established v1
conventions before implementation. This candidate does not impose a unique
name constraint because the existing v1 table has none. A future uniqueness
policy would require a separate contract and migration review.

No public operation count changes as a result of this document.
