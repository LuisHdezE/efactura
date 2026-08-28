# Interface Scope: Unresolved API Needs

## Purpose

These are architecture/API-design inputs discovered by the `SCOPE_BASELINE`. They are not endpoint names yet.

## Cross-cutting

- authoritative login/session/refresh/logout contract;
- permission and organization/location/terminal scope exposure without leaking security internals;
- consistent pagination/filter/sort conventions;
- RFC 9457 Problem Details and field-validation mapping;
- request/correlation IDs;
- idempotency contract for retry-sensitive actions;
- optimistic/concurrency conflict representation;
- offline sync batch/cursor/canonical-result contract;
- safe file/XML upload contract;
- document/PDF/XML download authorization;
- notification/alert summary contract.

## Sales/POS

- item/customer search optimized for POS;
- sale draft/validation/confirmation lifecycle;
- server-calculated totals/tax/fiscal eligibility preview;
- multiple payment media and credit-sale intent;
- fiscalization status independent from commercial confirmation;
- correction/credit-note/debit-note initiation;
- receipt/fiscal representation retrieval.

## Parties

- typed national/foreign fiscal identities;
- RUC/CI/passport/DNI/NIFE/other validation metadata;
- customer/supplier role management;
- credit/account summary without allowing UI-computed authoritative balance.

## Catalog/Inventory

- product/service common sellable search;
- stock position by location;
- immutable stock movement history;
- transfer/dispatch/receipt transitions;
- manual adjustment with permission/reason;
- EOQ/ROP simulation as advisory data, not direct stock mutation.

## Procurement/Finance

- PO lifecycle and receipt;
- received supplier fiscal evidence linkage;
- receivable/payable aging and allocations;
- partial payment/collection;
- overpayment/advance policy result;
- cash-shift open/close/reconcile/variance.

## Fiscal

- rule-explained CFE selection/eligibility;
- CAE import/status/range/allocation/alerts;
- CFE generation/transport/final-result states;
- correction/reference workflow;
- rejection/regularization work queue;
- CFC contingency registration/recovery;
- daily fiscal report state;
- received CFE/XML validation findings;
- export/cross-border tax-treatment evidence;
- e-Remito/e-Resguardo/e-Boleta Entrada/specialized families only when enabled.

## Audit/reporting/admin

- durable audit query/export with restricted permission;
- fiscal/sales/inventory/financial report data;
- fiscal calendar with provenance;
- health/integration/CAE/certificate/outbox status without exposing secrets;
- organization/location/terminal and integration configuration.

## Android-specific

- compact POS bootstrap/cache dataset;
- offline grant/device registration/revocation status;
- delta/cursor synchronization;
- deterministic replay/conflict/review result;
- local queue dependency ordering;
- limited offline customer/catalog/stock snapshots with freshness metadata;
- server reconciliation after connectivity recovery.

Every item above must be resolved in Architecture/API Contract Design or explicitly deferred. Missing authoritative behavior is a blocker, not permission for the future client to invent it.
