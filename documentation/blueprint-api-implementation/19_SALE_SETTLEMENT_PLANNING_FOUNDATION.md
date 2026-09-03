# 19 — Sale Settlement Planning Foundation

## Purpose

Introduce the deterministic financial-coverage boundary required before the transactional implementation of `API-SAL-007 confirmSale`.

This slice follows the accepted architecture rather than prematurely marking a sale confirmed. The canonical sale-confirmation sequence requires a payment/receivable effect before the commercial confirmation commits, while CAE allocation belongs to the later Fiscalization workflow, not directly to the sale-confirmation transaction.

## Canonical correction carried forward

`documentation/blueprint-architecture/05_APPLICATION_USE_CASES_AND_PORTS.md` defines sale confirmation as:

1. authorize and claim idempotency;
2. revalidate the Sale;
3. freeze the commercial snapshot;
4. create the required payment or receivable effect;
5. create stock effects only for tracked items;
6. create durable `FiscalizationRequested` state;
7. audit/outbox and commit;
8. process fiscal numbering/generation/signing/transport after that boundary.

The Fiscalization workflow subsequently allocates the CAE number atomically with fiscal-document identity persistence. Therefore this implementation line must not reserve a CAE directly inside `confirmSale`.

## Added foundation

`SaleSettlementPlanner` consumes the already accepted `SaleConfirmationPlan` instead of accepting a client-supplied total.

The amount to cover is always:

`SaleConfirmationPlan.FiscalCalculation.Totals.TotalAmount`.

The planner also binds the financial plan to:

- authoritative Sale ID and version;
- Sale validation fingerprint;
- confirmation fingerprint;
- organization and sale currency;
- authoritative payment-method ID/version/enabled evidence.

It emits a deterministic SHA-256 `SettlementFingerprint` so the future transaction can detect materially changed settlement evidence.

## Supported Release 1 planning modes

### Immediate payment

One or more positive payment intents may cover the authoritative total exactly.

Every payment intent must:

- reference an authoritative enabled payment method;
- use payment-method evidence from the same organization;
- carry a positive payment-method version;
- use the same currency as the sale.

Cross-currency settlement is deliberately rejected until an explicit FX settlement policy is accepted.

### Credit receivable

Credit terms currently carry only the due date needed for base receivable persistence.

The receivable amount is never accepted from the client. It is derived as:

`authoritative sale total - immediate payment total`.

Credit requires an identified customer and a due date on or after the sale business date.

This is compatible with OQ-006: advanced credit limits, approval policy, overpayment/advance behavior and similar commercial policy remain open, while base receivable persistence is explicitly not blocked.

### Mixed

Immediate payments may cover part of the total and the exact residual becomes the planned receivable.

### Zero total

A zero-total authoritative sale produces `NoCharge` and cannot create payment or receivable effects.

## Fail-closed boundaries

The planner rejects:

- stale sale/confirmation identity or version;
- mismatched validation evidence;
- invalid confirmation fingerprints;
- disabled, missing, cross-organization or unversioned payment-method evidence;
- non-positive payment amounts;
- cross-currency payment intents;
- uncovered balances without credit terms;
- credit without an identified customer;
- credit due dates before the sale business date;
- redundant credit terms when no residual remains;
- overpayment/customer advances until explicit policy exists.

## Explicit non-goals

This slice does **not**:

- expose `POST /api/v1/sales/{saleId}/confirm`;
- mark Sale `CONFIRMED`;
- persist `payments` or `receivables`;
- create payment allocations;
- implement credit limits/approvals/advances;
- classify a payment medium as cash/card/bank for custody effects;
- open/close or mutate a cash shift;
- consume stock;
- create `FiscalizationRequested` persistence;
- reserve CAE numbers;
- create FiscalDocument/XML/signature/transport;
- own UnitOfWork, idempotency, audit or outbox writes.

## Why PaymentMethod remains evidence-only here

The Brownfield `PaymentMethod` entity contains only an ID/name lifecycle and is not sufficient authority for target cash/card/bank semantics. The target contract does define payment-method resources, but classification and CashManagement consequences must be introduced deliberately rather than inferred from legacy names.

## Next slices

After this foundation is accepted:

1. introduce target PaymentMethod + Receivable/Payment persistence contracts and dual-provider evidence;
2. add the stock-consumption and `FiscalizationRequested` durable effects;
3. orchestrate the complete local `confirmSale` transaction with idempotency/audit/outbox;
4. expose `API-SAL-007` only when the accepted `SaleConfirmRequest` can be honored completely;
5. process CAE/fiscal-document identity in the separate Fiscalization workflow.

## Traceability

Primary sources:

- `documentation/blueprint-api-contract/07_REQUEST_RESPONSE_CONTRACTS.md` — `SaleConfirmRequest`, receivable and collection contracts;
- `documentation/blueprint-api-contract/02B_ENDPOINT_INVENTORY_OPERATIONS.md` — receivable/collection operations;
- `documentation/blueprint-architecture/02_MODULE_BOUNDARIES_AND_DEPENDENCIES.md` — Sales/Receivables/Treasury/Cash ownership;
- `documentation/blueprint-architecture/05_APPLICATION_USE_CASES_AND_PORTS.md` — sale confirmation and fiscalization transaction sequence;
- `documentation/blueprint-target/05_USE_CASE_LIFECYCLES_CORE.md` — cash sale, credit sale and collection lifecycles;
- `documentation/blueprint-target/08_TRACEABILITY_AND_OPEN_QUESTIONS.md` — OQ-006 credit-policy boundary.

## Governance

This document records an internal preparation boundary. It does not mark `API-SAL-007` implemented, does not mark OpenAPI/Postman/API QA gates complete and does not authorize a merge without owner approval.
