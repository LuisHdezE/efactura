# W2.4 Party Account Summary Runtime Closure

Status: `IMPLEMENTED / MERGED / CI_ACCEPTED / DEPLOYED / RUNTIME_ACCEPTED_READ_ONLY / CLOSED`

API ID: `API-PTY-008`

Operation ID: `getPartyAccountSummary`

Public surface:

```text
GET /api/v1/parties/{partyId}/account-summary
permission: parties.read
idempotency: NO
```

## 1. Governed lineage

- authoritative AR foundation merged in PR #230;
- authoritative AP foundation merged in PR #233;
- Application Party account-summary composer merged in PR #234;
- field-level HTTP contract locked in PR #236;
- HTTP implementation merged in PR #238 from exact approved HEAD `42e7fe30f26a1af7d338a70377b232cb2665930c`;
- implementation merge commit: `7f8281c9f98e2c0a9171fbc9d56ee5bd872eb60b`;
- pre-merge Clean Architecture Guard #776, run `35938239853`: SUCCESS, including PostgreSQL/MySQL provider-real persistence integration;
- post-merge Clean Architecture Guard #777, run `35945338526`: SUCCESS after rerunning the jobs cancelled by concurrent WebApp workflow activity, including PostgreSQL/MySQL provider-real persistence integration;
- Deploy eFactura API Demo #62, run `35945338530`: SUCCESS;
- accepted Cloud Run revision: `efactura-api-d22-7f8281c-62-1`;
- immutable image digest: `sha256:605e816500abab11fd7e068b307e22fd4c030d0fe7b9bfaa18f17a3911a4d0cb`;
- accepted revision promoted to 100% traffic after canary and public post-promotion smoke.

The required production schema prerequisites were already present in Neon before the HTTP merge gate and were independently verified:

- `20260923043000_V1ReceivableBalanceLedger`;
- `20260923143000_V1PayableBalanceFoundation`;
- both migration-history rows use EF ProductVersion `8.0.30`;
- all 3 governed W2.4 tables, 10 indexes and 5 foreign keys exist;
- `efactura_app` has SELECT/INSERT/UPDATE/DELETE on the three new tables.

## 2. Read-only production runtime evidence

Runtime acceptance targeted the accepted public service URL and revision after Deploy #62 completed successfully.

Observed production results:

| Check | Result |
|---|---|
| Swagger UI | HTTP 200 PASS |
| OpenAPI document | HTTP 200 PASS |
| Exact path | `GET /api/v1/parties/{partyId}/account-summary` PASS |
| Exact operationId | `getPartyAccountSummary` PASS |
| Declared responses | `200, 401, 403, 404` PASS |
| Request parameters | only required `partyId` path parameter PASS |
| Client-controlled `asOf` / date / currency | absent from OpenAPI PASS |
| Idempotency input | absent from OpenAPI PASS |
| Public summary DTO fields | exact governed field family PASS |
| Public currency DTO fields | `currencyCode,outstanding,overdue,aging` PASS |
| Public aging DTO fields | `current,days1To30,days31To60,days61To90,days91Plus` PASS |
| Request without JWT | HTTP 401 + `authentication_required` PASS |
| Authenticated actor without `parties.read` | HTTP 403 + `permission_denied` PASS |
| Organization scope escape | HTTP 403 + `organization_scope_denied` PASS |
| Authenticated unknown Party | HTTP 404 + `party.not_found` PASS |
| Problem Details media type | `application/problem+json` PASS |
| ETag on exercised error paths | absent PASS |
| Production business writes | NONE |

A read-only API query for `demo-org` returned `total = 0`. Independent read-only Neon inspection confirmed `v1_parties` contains zero rows across every organization in production.

## 3. Deliberate read-only limitation

The W2.4 prerequisite contract explicitly requires production runtime acceptance to remain read-only.

Because production contains zero Party rows, runtime could not honestly exercise a successful 200 account projection without manufacturing a Party plus AR/AP business fixture. No such mutation was authorized or performed.

The successful path remains covered by accepted exact-head QA:

- the WebApi controller test proves one server-controlled UTC `asOfUtc`, authoritative projection mapping, UYU receivables, USD payables, aging fields and `Cache-Control: private, no-store`;
- composer QA covers customer-only, supplier-only and dual-role applicability, deterministic currency ordering, UTC normalization and fail-closed financial invariants;
- provider-real PostgreSQL/MySQL QA creates a dual-role Party, a confirmed-sale receivable, a supplier payable, a collection allocation and a supplier-payment allocation, then reconstructs the final account summary through the real EF repositories and authoritative AR/AP read models;
- provider-real QA also proves cross-organization lookup fails with `party.not_found` before financial data can leak.

This closure therefore records W2.4 as `RUNTIME_ACCEPTED_READ_ONLY`. It does not claim that a production 200 business-state fixture was exercised.

## 4. Security and authority boundary

The accepted endpoint remains read-only and organization-scoped.

It does not:

- accept client-controlled organization or `asOf` inputs;
- perform FX conversion or AR/AP netting;
- derive balances from Party roles or Party master data;
- expose credit limits, risk scores, promises or recommendations;
- require idempotency;
- introduce a transaction/UoW/audit/outbox mutation path for the GET.

JWT signing material used by runtime acceptance was read from the existing Google Secret Manager secret, held only in process memory for short-lived tokens, never printed, and removed together with temporary response files after the checks.

## 5. Production data impact

Runtime acceptance itself produced:

```text
Schema writes: NONE
Business writes: NONE
Fixture creation: NONE
Audit writes: NONE
Outbox writes: NONE
Idempotency writes: NONE
```

The separately owner-approved AR/AP schema migrations predate this runtime closure and are not runtime-test writes.

## 6. Completion accounting

After this runtime closure is reconciled:

- Wave 2: `26 / 26` implemented HTTP surfaces;
- Wave 2 missing HTTP: `0`;
- global public v1: `68 / 194` implemented = `35.05%`;
- global missing HTTP: `124`;
- contract-collision IDs: `2`;
- total non-implemented IDs: `126`.

W2.2 `API-SAL-008 cancelSale` remains implemented, merged and deployed, with mutation-based production runtime acceptance still separately owner-gated because production has no suitable Sale fixture. That operational debt does not reduce the implemented HTTP count.

## 7. Next governed frontier

W2.4 closes the final missing Wave 2 HTTP surface and reconciles W2.5 HTTP completion.

After this reconciliation merges, the next public API implementation frontier is Wave 3: Payments + Cash + AR/AP. Wave 3 remains `0 / 24` and no Wave 3 operation is implied or exposed by the W2.4 AR/AP internal foundations.
