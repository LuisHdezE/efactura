# W2.4 Party Account Summary HTTP contract

Status: `CONTRACT_CANDIDATE_PENDING_OWNER_LOCK / IMPLEMENTATION_NOT_AUTHORIZED`

Baseline: `main@bb47649656d9476f1126455dcb1c220f7538b6bb` after owner-approved merge of PR #234.

API ID: `API-PTY-008`

Operation ID: `getPartyAccountSummary`

Accepted public surface:

```text
GET /api/v1/parties/{partyId}/account-summary
permission: parties.read
idempotency: NO
```

Parent prerequisite contract: `W2_4_PARTY_ACCOUNT_SUMMARY_PREREQUISITE_CONTRACT.md`.

This document locks only the field-level/public HTTP behavior for W2.4. It does **not** implement the route, wire WebApi DI, apply production migrations, mutate Neon production, deploy a new endpoint, advance completion counts, or authorize any Wave 3 HTTP surface.

## 1. Prerequisite closure represented in code

The prerequisite chain required before an HTTP contract could be locked is now represented in the repository:

1. authoritative AR balance/aging foundation;
2. authoritative AP balance/aging foundation;
3. provider-backed Application-owned `IPartyAccountSummaryReadModel` composer.

The composer is organization- and Party-scoped, role-aware only for applicability, currency-isolated, deterministic, and fail-closed on malformed authoritative projections.

The public HTTP surface MUST compose those authoritative Application results. It MUST NOT calculate balances inside the controller, query financial tables directly from WebApi, infer debt from Party roles, or reuse WebApp/mock data.

## 2. Request contract

The endpoint is a read-only GET with one public input:

```text
partyId: required GUID path parameter
```

W2.4 v1 exposes:

- no request body;
- no `Idempotency-Key` requirement;
- no client-controlled organization id in body or query;
- no client-controlled `asOf`, `asOfUtc`, date, currency or FX query parameter;
- no pagination parameter.

The organization is resolved from the authenticated request using the existing v1 organization-context boundary.

The server captures one UTC `asOf` instant for the request and passes that same instant to the Application read model. Clients cannot select a historical or future accounting cutoff through API-PTY-008 v1.

## 3. Authorization and organization isolation

The endpoint requires exactly:

```text
parties.read
```

It MUST NOT silently add `sales.read`, `payments.read`, `organization.read`, Wave 3 permissions or any mutation permission.

Existing global authentication/authorization semantics remain authoritative:

- unauthenticated -> existing `401` Problem Details behavior;
- authenticated without `parties.read` -> `403 permission_denied`;
- organization outside the actor company scope -> `403 organization_scope_denied`;
- missing Party or a Party id that belongs only to another organization -> `404 party.not_found` without cross-organization existence leakage.

The endpoint does not broaden Party visibility and does not allow organization selection through financial data.

## 4. Successful response contract

Successful response is HTTP `200` with this field-level shape:

```json
{
  "organizationId": "company-1",
  "partyId": "11111111-1111-1111-1111-111111111111",
  "asOfUtc": "2026-09-23T18:45:00Z",
  "receivablesApplicable": true,
  "receivables": [
    {
      "currencyCode": "UYU",
      "outstanding": 1250.5,
      "overdue": 250.5,
      "aging": {
        "current": 1000.0,
        "days1To30": 250.5,
        "days31To60": 0.0,
        "days61To90": 0.0,
        "days91Plus": 0.0
      }
    }
  ],
  "payablesApplicable": true,
  "payables": [
    {
      "currencyCode": "USD",
      "outstanding": 80.0,
      "overdue": 20.0,
      "aging": {
        "current": 60.0,
        "days1To30": 20.0,
        "days31To60": 0.0,
        "days61To90": 0.0,
        "days91Plus": 0.0
      }
    }
  ]
}
```

The exact CLR record names are implementation detail. The JSON field names and semantics in this section are the public v1 contract.

### 4.1 Top-level fields

- `organizationId`: organization resolved by the server;
- `partyId`: requested Party id;
- `asOfUtc`: the server-controlled UTC instant used for both AR and AP projections;
- `receivablesApplicable`: whether the Party has the CUSTOMER role and therefore the receivable side is applicable;
- `receivables`: authoritative receivable currency buckets;
- `payablesApplicable`: whether the Party has the SUPPLIER role and therefore the payable side is applicable;
- `payables`: authoritative payable currency buckets.

The applicability flags are intentionally public. They distinguish a side that does not apply to the Party from an applicable side whose current authoritative result contains no currency buckets.

## 5. Currency-bucket contract

Every receivable/payable bucket contains exactly:

```text
currencyCode
outstanding
overdue
aging.current
aging.days1To30
aging.days31To60
aging.days61To90
aging.days91Plus
```

Rules:

- `currencyCode` is canonical uppercase ISO alpha-3 as enforced by the authoritative Application projection;
- amounts are JSON numbers with the governed six-decimal monetary precision semantics; textual trailing zeroes are not contractually significant;
- all values are non-negative;
- `overdue <= outstanding`;
- `aging.current + aging.days1To30 + aging.days31To60 + aging.days61To90 + aging.days91Plus == outstanding` at governed precision;
- overdue aging buckets sum to `overdue` at governed precision;
- currencies remain isolated and MUST NOT be added together into one scalar balance;
- no FX conversion is introduced by W2.4.

Receivable and payable arrays are independently ordered by `currencyCode` using deterministic ordinal ordering.

## 6. Applicability and zero-state semantics

Party roles determine applicability only, never balance values.

### Customer only

```text
receivablesApplicable = true
payablesApplicable = false
payables = []
```

### Supplier only

```text
receivablesApplicable = false
receivables = []
payablesApplicable = true
```

### Dual role

Both applicability flags are `true` and both authoritative projections are required.

### Applicable side with no obligations

An applicable side may return an empty currency array when there is no authoritative obligation history for that side.

The endpoint MUST NOT fabricate a zero-value currency bucket merely because the Party has CUSTOMER or SUPPLIER role.

If the authoritative projection preserves a zero-balance bucket because durable obligation history exists, that bucket may be returned exactly as produced by the Application read model.

## 7. Aging semantics

W2.4 preserves the accepted prerequisite aging rules:

- `current`: due date on or after the server-controlled `asOfDate`;
- `days1To30`: 1 to 30 calendar days overdue;
- `days31To60`: 31 to 60 calendar days overdue;
- `days61To90`: 61 to 90 calendar days overdue;
- `days91Plus`: 91 or more calendar days overdue.

Only remaining outstanding amount participates in aging.

The HTTP layer MUST NOT recalculate aging independently. It maps the authoritative Application projection.

## 8. Problem Details and fail-closed behavior

Expected public failures are:

| Condition | HTTP | Stable code / behavior |
|---|---:|---|
| no valid authentication | `401` | existing authentication Problem Details |
| missing `parties.read` | `403` | `permission_denied` |
| organization-scope escape | `403` | `organization_scope_denied` |
| Party missing in resolved organization | `404` | `party.not_found` |

Malformed authoritative financial state, scope mismatches, duplicate currencies, negative balances or aging reconciliation failures MUST NOT be converted into a partial or fabricated `200` response. The request fails closed through the existing server-error Problem Details boundary and must not expose internal persistence details.

No side may be silently omitted when its applicability flag is `true` because a dependency is unavailable or inconsistent.

## 9. Freshness and caching

The response represents a server-controlled point-in-time financial projection.

W2.4 v1 locks:

```text
Cache-Control: private, no-store
```

Rationale:

- Party financial data is actor-scoped and must never be shared through intermediary caches;
- balance/aging may change after allocations, reversals or adjustments;
- `asOfUtc` is part of the response and is captured per request.

W2.4 v1 does not define ETag/304 semantics. A future cache contract requires a separately governed version/revision authority and must not be inferred from `asOfUtc` alone.

## 10. Explicit exclusions

The endpoint does not expose or calculate:

- credit limit;
- available credit;
- risk score;
- collection promises;
- payment recommendations;
- consolidated/net balance across currencies;
- FX rates or converted totals;
- receivable/payable transaction history;
- invoice or fiscal-document lists;
- payment/collection command operations;
- adjustment command operations;
- Party master-data mutation;
- Wave 3 public AR/AP endpoints.

It also does not expose internal effect ids, source ids, persistence keys, confirmation fingerprints, settlement fingerprints, database metadata or provider-specific values.

## 11. Required implementation evidence

Before implementation merge approval, automated QA must prove at minimum:

### Contract / OpenAPI

- exact GET route `/api/v1/parties/{partyId}/account-summary`;
- operationId `getPartyAccountSummary`;
- exact permission `parties.read`;
- no idempotency requirement;
- no request body and no client `asOf` query surface;
- exact public field set from this contract;
- `Cache-Control: private, no-store`;
- no ETag requirement.

### Authorization / isolation

- no JWT -> existing `401` behavior;
- authenticated without `parties.read` -> `403 permission_denied`;
- company-scope escape -> `403 organization_scope_denied`;
- missing/cross-organization Party -> `404 party.not_found`;
- no cross-organization financial leakage.

### Projection

- customer-only applicability;
- supplier-only applicability;
- dual-role behavior;
- applicable side with no obligations;
- per-currency separation;
- deterministic currency ordering;
- server-controlled `asOfUtc` propagated consistently to both sides;
- no implicit FX/netting;
- no fabricated zero bucket;
- six-decimal reconciliation behavior preserved;
- malformed authoritative bucket fails closed rather than returning partial data.

### Regression / architecture

- existing `API-PTY-001..007` behavior remains unchanged;
- no transaction/UoW/audit/outbox/idempotency mutation dependency is introduced for this GET;
- controller maps `IPartyAccountSummaryReadModel` and does not implement financial calculations;
- provider-real PostgreSQL/MySQL composer/persistence tests remain green.

## 12. Production migration and rollout gate

The AR/AP foundations introduced additive migrations that are committed and provider-real tested but are not yet authorized/applied to Neon production:

```text
20260923043000_V1ReceivableBalanceLedger
20260923143000_V1PayableBalanceFoundation
```

The HTTP implementation MUST NOT be considered production-ready while those schema prerequisites are absent.

Before an implementation merge that would automatically deploy the public endpoint, the production sequence is:

1. implementation PR reaches exact-head green CI/provider-real gate;
2. owner gives separate explicit approval to apply the two W2.4 prerequisite migrations to Neon production;
3. migrations are applied and verified without exposing secrets;
4. implementation merge receives its own explicit exact-head approval;
5. automatic API deploy completes successfully;
6. read-only production runtime acceptance validates the endpoint;
7. only then are Wave 2 and global operation counts reconciled.

This contract candidate does not authorize step 2 or any other production mutation.

## 13. Completion accounting

Contract lock alone does not implement the operation.

Until the HTTP implementation is merged, deployed and runtime-accepted:

- Wave 2 remains `25 / 26` implemented HTTP surfaces;
- global public v1 remains `67 / 194` implemented;
- `API-PTY-008 getPartyAccountSummary` remains `MISSING_HTTP`.

After successful runtime acceptance, the reconciliation increment may advance:

```text
Wave 2: 26 / 26
Global public v1: 68 / 194
```

No Wave 3 operation count advances merely because W2.4 reuses AR/AP foundations.

## 14. Contract-lock effect

Owner-approved merge of this document will authorize only a later bounded implementation increment for `API-PTY-008` under the normal exact-head CI, production-migration, deployment and runtime gates.

It does not authorize production migrations by itself, does not authorize runtime mutation, and does not authorize any Wave 3 HTTP endpoint.
