# W2.4 AP balance foundation

Status: `IMPLEMENTED_PENDING_REVIEW`

Parent prerequisite contract: `documentation/api-completion-matrix/W2_4_PARTY_ACCOUNT_SUMMARY_PREREQUISITE_CONTRACT.md`.

This increment implements only the **accounts-payable authoritative balance foundation** required by W2.4. It does not implement `API-PTY-008 getPartyAccountSummary`, does not expose any Wave 3 HTTP operation and does not advance API completion counts.

## Implemented foundation

### Domain

`Payable` is an explicit supplier obligation. It can be created only from a governed business-source category represented by:

- `PurchaseReceipt`, for an accepted procurement receipt business event;
- `ReceivedFiscalDocument`, for an accepted received-fiscal-document/accounting event.

The model preserves source kind and source identity independently of Party roles. A Party being a supplier is therefore **not sufficient** to create debt.

`PayableBalanceEffect` is an append-only financial fact with explicit kinds:

- adjustment increase;
- adjustment decrease;
- supplier-payment allocation;
- supplier-payment reversal.

Amounts are positive facts. Their signed contribution to outstanding balance is determined by the effect kind, not by caller-provided signed values.

Supplier-payment reversals reference the allocation effect they reverse. Application projection rejects missing targets, wrong target kinds, reversal-before-allocation ordering and amount mismatches.

### Application

`PartyPayableAccountReadModel` owns payable account projection semantics over a Party-scoped source reader.

It provides:

- organization and Party scope validation;
- deterministic per-currency buckets;
- outstanding amount derived from the original obligation plus durable effects;
- exclusion of future effects from an `asOfUtc` snapshot;
- fail-closed negative-balance protection, preventing silent overpayment truncation;
- overdue totals;
- aging buckets at current, 1-30, 31-60, 61-90 and 91+ calendar days;
- deterministic six-decimal normalization matching persistence precision.

Fully settled currency buckets remain visible with zero totals when obligations in that currency exist. Currencies are never consolidated through implicit FX conversion.

### Persistence

`v1_payables` stores the authoritative obligation and preserves:

- organization and supplier Party scope;
- governed source kind and source identity;
- original amount and currency;
- source business date and due date;
- version and creation timestamp.

A unique organization/source-kind/source-id constraint prevents the same accepted business source from creating duplicate payable obligations.

`v1_payable_balance_effects` stores the append-only balance ledger and preserves:

- organization scope;
- payable identity;
- effect kind and amount;
- lifecycle source id and sequence;
- optional reversed allocation effect id;
- occurrence timestamp.

Database constraints/indexes enforce:

- one lifecycle source fact per organization/kind/source/sequence;
- at most one persisted reversal for a supplier-payment allocation effect;
- payable and supplier Party foreign-key integrity;
- query support by organization/supplier/due date and organization/payable/time.

`EfPayableRepository` owns payable obligation persistence. `EfPayableBalanceRepository` implements both the effect repository and Party-scoped balance-source reader. The persistence query applies organization and Party predicates before materialization.

## QA

Cross-cutting tests cover:

- explicit purchase-receipt and received-fiscal-document source categories;
- open, partially settled and fully settled balances;
- adjustments;
- supplier-payment allocations and reversals;
- future-effect exclusion;
- fail-closed over-allocation;
- invalid reversal amounts;
- Party scope mismatch;
- currency isolation;
- exact aging boundaries at 0/1/30/31/60/61/90/91 days.

Provider-real persistence tests execute on PostgreSQL and MySQL and cover:

- full migration chain into a fresh database;
- Payable source persistence;
- duplicate-source rejection at the database boundary;
- append-only balance-effect persistence;
- Party/organization-scoped reconstruction;
- currency bucket reconstruction;
- double-reversal rejection at the database boundary.

## Deliberate non-scope

This increment does **not** provide:

- purchase-order or goods-receipt HTTP operations;
- received-CFE intake HTTP operations;
- a public payables HTTP API;
- supplier-payment commands;
- payable-adjustment commands;
- public aging endpoints;
- the W2.4 Party account-summary composer;
- W2.4 WebApi DTOs/controller;
- production migration execution.

Future Wave 3 procurement, received-document and supplier-payment commands must write this same AP truth rather than introduce another payable balance store.

## Completion accounting

Wave 2 remains `25 / 26` implemented HTTP surfaces.

Global accepted completion remains `67 / 194`.

With AR and AP foundations represented in code, `API-PTY-008 getPartyAccountSummary` still remains `MISSING_HTTP / PREREQUISITE_REQUIRED` until the application composer and its governed contract are implemented and reviewed.

No production migration is authorized by this implementation increment. The migration is committed for provider-real CI testing only; promotion to Neon production remains a separate explicit owner gate.
