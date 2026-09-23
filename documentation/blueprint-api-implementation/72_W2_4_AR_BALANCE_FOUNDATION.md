# W2.4 AR balance foundation

Status: `IMPLEMENTED_PENDING_REVIEW`

Parent prerequisite contract: `documentation/api-completion-matrix/W2_4_PARTY_ACCOUNT_SUMMARY_PREREQUISITE_CONTRACT.md`.

This increment implements only the **accounts-receivable authoritative balance foundation** required by W2.4. It does not implement `API-PTY-008 getPartyAccountSummary`, does not expose any Wave 3 HTTP operation and does not advance API completion counts.

## Implemented foundation

### Domain

`ReceivableBalanceEffect` is an append-only financial fact with explicit kinds:

- adjustment increase;
- adjustment decrease;
- collection allocation;
- collection reversal.

Amounts are positive facts. Their signed contribution to outstanding balance is determined by the effect kind, not by caller-provided signed values.

Collection reversals reference the allocation effect they reverse. Application projection rejects missing targets, wrong target kinds, reversal-before-allocation ordering and amount mismatches.

### Application

`PartyReceivableAccountReadModel` owns receivable account projection semantics over a Party-scoped source reader.

It provides:

- organization and Party scope validation;
- deterministic per-currency buckets;
- outstanding amount derived from the original obligation plus durable effects;
- exclusion of future effects from an `asOfUtc` snapshot;
- fail-closed negative-balance protection;
- overdue totals;
- aging buckets at current, 1-30, 31-60, 61-90 and 91+ calendar days;
- deterministic six-decimal normalization matching the existing persistence precision.

Fully settled currency buckets remain visible with zero totals when obligations in that currency exist. Currencies are never consolidated through implicit FX conversion.

### Persistence

`v1_receivable_balance_effects` stores the append-only ledger and preserves:

- organization scope;
- receivable identity;
- effect kind and amount;
- lifecycle source id and sequence;
- optional reversed allocation effect id;
- occurrence timestamp.

Database constraints/indexes enforce:

- one lifecycle source fact per organization/kind/source/sequence;
- at most one persisted reversal for an allocation effect;
- receivable foreign-key integrity;
- query support by organization/receivable/time.

`EfReceivableBalanceRepository` implements both the effect repository and the Party-scoped balance-source reader. The persistence query applies organization and Party predicates before materialization.

## QA

Cross-cutting tests cover:

- open, partially settled and fully settled balances;
- adjustments;
- collection allocations and reversals;
- future-effect exclusion;
- fail-closed over-collection;
- invalid reversal amounts;
- Party scope mismatch;
- currency isolation;
- exact aging boundaries at 0/1/30/31/60/61/90/91 days.

Provider-real persistence tests execute on PostgreSQL and MySQL and cover:

- full migration chain into a fresh database;
- append-only effect persistence;
- Party/organization-scoped reconstruction;
- currency bucket reconstruction;
- double-reversal rejection at the database boundary.

## Deliberate non-scope

This increment does **not** provide:

- a public receivables HTTP API;
- collection commands;
- receivable-adjustment commands;
- public aging endpoints;
- Payable/AP state;
- supplier payments;
- the W2.4 Party account-summary composer;
- W2.4 WebApi DTOs/controller;
- production migration execution.

Future Wave 3 commands must write the same append-only AR truth rather than introduce another balance store.

## Completion accounting

Wave 2 remains `25 / 26` implemented HTTP surfaces.

Global accepted completion remains `67 / 194`.

`API-PTY-008 getPartyAccountSummary` remains `MISSING_HTTP / PREREQUISITE_REQUIRED` because the AP authoritative balance foundation is still absent.

No production migration is authorized by this implementation increment. The migration is committed and provider-real tested only; promotion to Neon production remains a separate explicit owner gate.
