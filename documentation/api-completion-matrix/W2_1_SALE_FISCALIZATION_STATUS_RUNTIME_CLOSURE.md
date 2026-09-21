# W2.1 Sale Fiscalization Status Runtime Closure

Status: `IMPLEMENTED / MERGED / CI_ACCEPTED / DEPLOYED / RUNTIME_ACCEPTED_READ_ONLY / CLOSED`

API ID: `API-SAL-009`

Operation ID: `getSaleFiscalizationStatus`

Public surface:

```text
GET /api/v1/sales/{saleId}/fiscalization
permission: sales.read
```

## 1. Governed lineage

- field-level contract locked by PR #207;
- implementation merged by PR #208;
- implementation merge commit: `9989c0f15d76423f76e25e1cfdc1ab1337586849`;
- pre-merge Clean Architecture Guard #695: SUCCESS;
- post-merge Clean Architecture Guard #696: SUCCESS, including PostgreSQL/MySQL transactional persistence integration;
- Deploy API Demo #55: SUCCESS;
- accepted Cloud Run revision: `efactura-api-d22-9989c0f-55-1`;
- accepted revision promoted to 100% traffic after canary and public post-promotion smoke;
- runtime acceptance harness merged by PR #210;
- runtime acceptance merge commit: `660ad188ee919d00fcc00626afbf57ead9067988`;
- production runtime acceptance run: `35643849728`;
- runtime job: `106479398308`;
- runtime conclusion: SUCCESS.

No W2.1 migration or schema change exists.

## 2. Production runtime evidence

The one-shot acceptance verified the exact accepted Cloud Run revision before exercising the endpoint.

Observed production results:

| Check | Result |
|---|---|
| OpenAPI document | HTTP 200 |
| Exact `GET /api/v1/sales/{saleId}/fiscalization` operationId | `getSaleFiscalizationStatus` PASS |
| POST/PUT/PATCH/DELETE companion surface | absent PASS |
| Request without JWT | HTTP 401 PASS |
| Malformed JWT | HTTP 401 PASS |
| Authenticated actor without `sales.read` | HTTP 403 PASS |
| Organization scope escape | HTTP 403 + `organization_scope_denied` PASS |
| Authenticated unknown sale | HTTP 404 + `sales.not_found` PASS |

The runtime harness performed no production mutation and created no fixture.

## 3. Deliberate read-only limitation

A read-only Neon inspection immediately before the runtime harness confirmed production contained zero rows in `v1_sales`.

Therefore production runtime could not honestly exercise successful 200 projections for `NOT_REQUESTED`, `PENDING`, or `IDENTITY_CREATED` without manufacturing a Sale/fiscalization fixture. No such mutation was authorized or performed.

The successful-state projection remains covered by the accepted automated QA from PR #208:

- Draft without request -> `NOT_REQUESTED`;
- Validated without request -> `NOT_REQUESTED`;
- Confirmed + Pending request -> `PENDING`;
- Confirmed + matching immutable fiscal identity -> `IDENTITY_CREATED`;
- impossible durable combinations -> `409 fiscalization.inconsistent_state`.

This closure therefore records runtime acceptance specifically as `RUNTIME_ACCEPTED_READ_ONLY`; it does not claim a production 200 business-state fixture was exercised.

## 4. Security and authority boundary

The accepted endpoint remains read-only and company-scoped.

Every successful projection is local workflow authority only and carries:

```text
statusAuthority = LOCAL_WORKFLOW_ONLY_NOT_DGI_ACCEPTANCE
```

W2.1 does not claim DGI/provider submission, receipt, acceptance, rejection, acknowledgement or transport state.

JWT signing material used by the one-shot runner was retrieved through the existing GitHub OIDC / Google WIF path, masked, used only for short-lived test tokens and removed during runner cleanup.

## 5. Production data impact

```text
Schema writes: NONE
Business writes: NONE
Fixture creation: NONE
Audit writes: NONE
Outbox writes: NONE
Idempotency writes: NONE
```

The one-shot workflow is removed by the same closure increment after evidence capture.

## 6. Completion accounting

W2.1 does not change the already reconciled operation totals beyond the HTTP implementation that PR #208 introduced:

- Wave 2: `23 / 26` implemented;
- Wave 2 missing HTTP: `3`;
- global public v1: `65 / 194` implemented = `33.51%`;
- global missing HTTP: `127`;
- contract-collision IDs: `2`;
- total non-implemented IDs: `129`.

## 7. Next governed frontier

The next Wave 2 increment is `W2.2 / API-SAL-008 cancelSale`.

Its prerequisite remains unchanged: the Sale cancellation lifecycle, cancellable states, irreversible confirmation boundary, reason/version contract and durable evidence semantics must be locked before implementation.
