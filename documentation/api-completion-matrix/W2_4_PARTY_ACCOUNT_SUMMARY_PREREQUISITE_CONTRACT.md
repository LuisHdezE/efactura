# W2.4 Party Account Summary prerequisite contract

Status: `CONTRACT_CANDIDATE / IMPLEMENTATION_NOT_AUTHORIZED`

Baseline: `main@d80eb1b3f2cf8768ec2a21ea3336aa150b68a41c`.

Target operation:

```text
API-PTY-008 getPartyAccountSummary
GET /api/v1/parties/{partyId}/account-summary
permission: parties.read
idempotency: NO
```

This document locks the prerequisite boundary for W2.4. It deliberately does **not** authorize the HTTP endpoint, schema changes, production migrations, deployment, runtime mutations, or any fabricated balance derived from Party master data.

## 1. Why W2.4 is not implementation-ready yet

The repository currently has a real `Receivable` model created from confirmed sales, but that model records the original obligation only. It does not currently own an outstanding-balance lifecycle.

Current `Receivable` evidence contains:

- `CustomerPartyId`;
- `SaleId`;
- `OriginalAmount`;
- `CurrencyCode`;
- `DueDate`;
- immutable confirmation/settlement fingerprints;
- version and creation timestamp.

The current `IReceivableRepository` can load by receivable ID or sale ID and can add a receivable. It does not provide Party-scoped aggregation.

More importantly, the current backend does not yet contain authoritative governed state for:

- collection allocations against receivables;
- collection reversals;
- receivable adjustments;
- receivable outstanding balance after those events;
- Party-scoped receivable aging;
- a Payable domain model;
- payable adjustments;
- supplier-payment allocations and reversals;
- Party-scoped payable aging.

Wave 3 already inventories those missing AR/AP lifecycles and projections. W2.4 must reuse that future truth rather than introduce a parallel accounting system.

## 2. Fail-closed rule

`API-PTY-008` MUST NOT be implemented by any of the following shortcuts:

- summing `Receivable.OriginalAmount` and calling it outstanding balance;
- treating every existing receivable as unpaid forever;
- returning supplier/payable balance as zero merely because no Payable model exists;
- calculating financial values from Party roles or Party master fields;
- reading visual-preview/WebApp mock data;
- creating a controller-local or Infrastructure-only SQL aggregation that bypasses the governed AR/AP lifecycle;
- mixing currencies into one scalar total without an explicit conversion source and rate contract.

If authoritative financial state for an applicable account side is absent, the implementation must remain blocked rather than manufacture a value.

## 3. Minimal authoritative read-model prerequisite

Before the public HTTP endpoint exists, Application must own a Party-scoped read port with semantics equivalent to:

```text
IPartyAccountSummaryReadModel
  GetAsync(organizationId, partyId, asOfUtc)
```

The concrete implementation may evolve, but the read model must satisfy all rules below.

### 3.1 Organization and Party scope

- `organizationId` is resolved from the authenticated actor/context, never trusted from the response request body;
- the Party must belong to that organization;
- cross-organization Party IDs must not leak existence;
- the projection must be Party-scoped at the persistence/query boundary, not filtered after loading another organization's data.

### 3.2 Applicable account sides

Party roles determine applicability only, never balances:

- `CUSTOMER` role makes the receivable side applicable;
- `SUPPLIER` role makes the payable side applicable;
- a dual-role Party requires both sides to be authoritative before W2.4 may claim a complete commercial account summary;
- a side that is not applicable is distinct from an applicable side whose financial projection is unavailable.

### 3.3 Currency isolation

Financial totals must be returned as currency buckets.

No implementation may add UYU, USD or any other currencies into a single total unless a separately governed FX conversion contract exists. W2.4 does not introduce such an FX contract.

Each currency bucket must therefore remain independently authoritative.

### 3.4 Outstanding-balance source of truth

For receivables, outstanding balance must reflect the governed obligation lifecycle, including all applicable source obligations and subsequent adjustments, collection allocations and reversals.

For payables, outstanding balance must mirror the same principle using governed supplier obligations, adjustments, supplier-payment allocations and reversals.

The projection may be materialized or calculated, but it must be reconstructible from durable authoritative state and must not depend on mutable UI state.

### 3.5 Aging semantics

Aging is evaluated using the obligation due date against a server-controlled `asOf` business date.

The initial governed buckets are:

- `current`: due date is on or after `asOfDate`;
- `days1To30`: 1 to 30 calendar days overdue;
- `days31To60`: 31 to 60 calendar days overdue;
- `days61To90`: 61 to 90 calendar days overdue;
- `days91Plus`: 91 or more calendar days overdue.

Only the **remaining outstanding amount** participates in aging. Settled obligations must not remain in aging totals.

The eventual HTTP response must expose the authoritative `asOf` value used by the server.

## 4. Candidate account-summary projection

Once both applicable sides are authoritative, the public W2.4 DTO may be locked around this shape:

```text
organizationId
partyId
asOfUtc
receivables[] by currency:
  currencyCode
  outstanding
  overdue
  aging:
    current
    days1To30
    days31To60
    days61To90
    days91Plus
payables[] by currency:
  currencyCode
  outstanding
  overdue
  aging:
    current
    days1To30
    days31To60
    days61To90
    days91Plus
```

This is a prerequisite projection candidate, not yet the accepted field-level HTTP response contract.

W2.4 intentionally excludes until separately governed:

- credit limit;
- available credit;
- risk score;
- collection promises;
- payment recommendations;
- FX-converted consolidated totals;
- ledger transaction detail;
- invoice/document lists.

## 5. Minimal prerequisite implementation sequence

W2.4 must not force implementation of the entire Wave 3 HTTP surface. The smallest safe dependency chain is:

1. **AR authoritative balance foundation**
   - extend receivable financial truth so outstanding amount can reflect governed adjustments, collection allocations and reversals;
   - add Party-scoped/currency-scoped read projection and aging;
   - do not expose Wave 3 public HTTP operations merely to satisfy W2.4.
2. **AP authoritative balance foundation**
   - introduce the governed Payable obligation model and supplier-payment/adjustment/reversal truth needed for Party-scoped outstanding balance and aging;
   - source payable creation from an accepted procurement/accounting business event, never from Party role alone.
3. **Application Party account-summary composition**
   - compose only authoritative AR/AP projections;
   - enforce organization and Party boundaries;
   - preserve currency isolation and deterministic ordering.
4. **W2.4 field-level HTTP contract lock**
   - finalize response fields and Problem Details semantics;
   - add exact route/operationId/permission tests.
5. **W2.4 HTTP implementation + runtime acceptance**
   - implement the read-only endpoint;
   - no idempotency;
   - no financial mutation;
   - production runtime acceptance must be read-only.

## 6. Relationship to Wave 3

This prerequisite deliberately aligns with, but does not automatically complete, the accepted Wave 3 operations:

- `API-AR-001/002/003` receivable reads/aging;
- `API-AR-004` receivable adjustments;
- `API-COL-001/002/003` collections and reversals;
- `API-AP-001/002/003` payable reads/aging;
- `API-AP-004` payable adjustments;
- `API-PAY-001/002/003` supplier payments and reversals.

Reusable Domain/Application/Persistence foundations created for W2.4 may later satisfy part of those operations, but the Wave 3 operation counts must not advance until their own public HTTP contracts and acceptance gates are actually implemented.

## 7. QA gates for the prerequisite foundation

Any code increment implementing this prerequisite must prove at minimum:

- organization isolation;
- Party scope isolation;
- customer-only, supplier-only and dual-role behavior;
- per-currency separation;
- zero/open/partially-settled/fully-settled obligations;
- adjustment effects;
- allocation effects;
- reversal effects;
- aging boundary dates at 0/1/30/31/60/61/90/91 days;
- deterministic rounding under the existing money/currency rules;
- provider-real PostgreSQL/MySQL persistence behavior where schema/query behavior changes;
- no weakening of existing Party/Sales/Settlement regressions.

No production migration or runtime mutation is authorized by this contract candidate.

## 8. Exit criterion

W2.4 becomes `IMPLEMENTATION_READY_PENDING_HTTP_CONTRACT_LOCK` only when a provider-backed Party-scoped AR/AP read model can produce complete, authoritative outstanding and aging values for every account side applicable to the Party.

Until then `API-PTY-008` remains `MISSING_HTTP / PREREQUISITE_REQUIRED`.