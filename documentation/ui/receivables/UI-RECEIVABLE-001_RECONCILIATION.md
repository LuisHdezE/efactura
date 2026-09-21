# UI-RECEIVABLE-001 — Reconciliation

Status: `RECONCILED / SPECIFICATION_READY / EXECUTION_BLOCKED_BY_API`

## Mapping

```text
WEB-010 -> UI-RECEIVABLE-001
```

Reserved route:

```text
/cuentas-por-cobrar
```

Navigation state: `PLANNED_DISABLED`

## 1. Purpose

Reconcile the proposed `WEB-010 — Accounts Receivable and Collections` interface against the current requirements, use cases, accepted API contracts and executable backend evidence before visual or React work proceeds.

This reconciliation authorizes a governed functional specification and visual drafting only. It does not authorize an executable route or live collection behavior.

## 2. Authoritative upstream scope

The interface baseline defines `WEB-010` as a financial workspace to:

- view receivables, open balances and aging;
- inspect allocation history;
- register partial/full customer collections;
- keep currency/balance presentation accessible and responsive.

Primary roles are treasury, seller and administrator. Authorization remains permission-based rather than role-name based.

## 3. Requirements traceability

The boundary is supported by:

- `FR-023`: a credit sale creates/links a receivable after the proper commercial/fiscal boundary;
- `FR-026`: payment is an independent durable record capable of allocation to one or more obligations according to policy;
- `FR-027`: partial payments/collections preserve allocation history and derived balances;
- `FR-070`: receivables carry due dates, aging, original amounts, allocations and derived balances;
- `FR-072`: partial/full customer collections are supported without silently truncating overpayments;
- `FR-073`: overpayment/advance handling follows explicit configured policy;
- `FR-074`: projected cash-flow data may derive from receivables/payables and approved planned events, but the complete cash-flow/reporting surface is not owned by this view.

## 4. Use-case authority

### `UC-SALE-003 — Confirm credit sale and create receivable`

This establishes the upstream receivable obligation and requires:

- due date/terms;
- original amount/currency;
- balance derived from allocations rather than destructive amount edits;
- aging/status concepts such as open, partial, settled and overdue.

### `UC-AR-001 — Record customer collection and allocate payment`

The collection lifecycle requires:

1. create a payment/collection with external reference/payment medium;
2. persist it idempotently;
3. allocate all/part to one or more receivables according to policy;
4. recalculate open balances from allocations;
5. generate a receipt/document representation when business policy requires it;
6. update cash-shift expected totals when applicable;
7. audit payment and allocations.

The explicit invariant is that overpayment/advance requires policy and must not be silently truncated.

## 5. Accepted API contract

| API ID | operationId | Method / path | Permission | Idempotency |
| --- | --- | --- | --- | --- |
| `API-AR-001` | `listReceivables` | GET `/api/v1/receivables` | `receivables.read` | NO |
| `API-AR-002` | `getReceivable` | GET `/api/v1/receivables/{receivableId}` | `receivables.read` | NO |
| `API-AR-003` | `getReceivablesAging` | GET `/api/v1/receivables/aging` | `receivables.read` | NO |
| `API-AR-004` | `createReceivableAdjustment` | POST `/api/v1/receivables/{receivableId}/adjustments` | `receivables.adjust` | REQUIRED |
| `API-COL-001` | `createCollection` | POST `/api/v1/collections` | `receivables.collect` | REQUIRED |
| `API-COL-002` | `getCollection` | GET `/api/v1/collections/{collectionId}` | `receivables.read` | NO |
| `API-COL-003` | `reverseCollection` | POST `/api/v1/collections/{collectionId}/reverse` | `receivables.collect` | REQUIRED |

The accepted contract describes read models, aging, durable collections, allocations, adjustments and compensating reversals. It does not authorize destructive rewriting of financial history.

## 6. Current executable evidence

Wave 3 currently records all seven operations above as:

```text
Implementation: MISSING_HTTP
Current WebApi evidence: none
Deep readiness: NOT_YET_AUDITED
```

Therefore accepted contract design is not executable WebApi capability.

Existing domain/persistence foundations for receivables do not change this HTTP gate.

## 7. Permission boundary

The relevant permissions are:

- `receivables.read`: read customer obligations, aging and collections;
- `receivables.adjust`: append authorized receivable adjustments;
- `receivables.collect`: create/reverse collections and allocations.

Visual action placement may reflect these permissions conceptually, but an executable frontend must use backend authorization results rather than infer authority from role labels.

## 8. Audit and financial-history rules

Accepted audit mapping includes:

- `createReceivableAdjustment -> receivable.adjusted`;
- `createCollection -> collection.created` plus allocation details;
- reversal must be compensating financial history, not record deletion.

The UI must preserve the distinction between:

- original obligation amount;
- allocations;
- adjustments;
- derived open balance;
- collection/reversal history.

## 9. Reconciled visual boundary

A visual candidate may represent, with clearly illustrative data while HTTP is absent:

- receivables list/search/filtering;
- aging summary/buckets;
- due dates, original amounts and derived balances;
- selected receivable detail;
- allocation/collection history;
- partial/full collection composition;
- allocation across one or multiple receivables;
- adjustment and compensating reversal affordance locations;
- explicit overpayment/advance result area without inventing the policy result;
- permission-aware action placement;
- desktop/tablet/mobile and light/dark states.

## 10. Explicit exclusions

This view must not become authority for:

- customer master-data editing;
- supplier payables or supplier payments;
- cash-shift opening/closing/reconciliation;
- bank reconciliation;
- generic accounting journal editing;
- destructive deletion of receivables, allocations or collections;
- local recalculation that overrides server balances;
- silent overpayment truncation;
- invented advance-credit policy;
- tax/fiscal-document mutation;
- full cash-flow reporting/forecasting owned by reporting/dashboard surfaces;
- live actions while the seven required API operations remain `MISSING_HTTP`.

## 11. Decision

`WEB-010` has a sufficiently stable boundary to receive the governed identifier `UI-RECEIVABLE-001` and reserved route `/cuentas-por-cobrar`.

Execution remains blocked. The next authorized artifact is the functional specification followed by a responsive light/dark visual candidate.

Before any React route activation, the API lane must provide executable evidence for the required receivables/collections operations and this UI must be reconciled again against the actual DTOs, errors, permissions and concurrency/idempotency behavior.