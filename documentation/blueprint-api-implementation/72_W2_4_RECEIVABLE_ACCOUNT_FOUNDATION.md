# W2.4 Receivable account foundation

Status: `IMPLEMENTED_PENDING_REVIEW`

Parent prerequisite contract: `documentation/api-completion-matrix/W2_4_PARTY_ACCOUNT_SUMMARY_PREREQUISITE_CONTRACT.md`.

This increment implements only step 1 of the W2.4 prerequisite sequence: the authoritative AR balance foundation. It does not implement `API-PTY-008`, does not expose any Wave 3 HTTP operation and does not authorize a production migration.

## Scope

The increment adds an immutable receivable balance-fact ledger with four governed fact kinds:

- adjustment increase;
- adjustment decrease;
- collection allocation;
- collection reversal.

The original receivable remains the source obligation. Outstanding balance is derived from that obligation plus durable facts rather than stored as an independently mutable scalar.

A provider-backed `IReceivableAccountReadModel` now projects a Party's receivable position by currency and server-supplied `asOfDate`, returning:

- outstanding;
- overdue;
- current;
- 1-30 days overdue;
- 31-60 days overdue;
- 61-90 days overdue;
- 91+ days overdue.

Fully settled obligations are excluded from aging. Currency buckets are never combined.

## Integrity rules

`EfReceivableAccountStore` fails closed when:

- the receivable is outside the requested organization;
- a fact would drive the balance below zero at any effective-date boundary;
- a collection reversal does not reference an existing allocation on the same receivable;
- a reversal amount differs from the referenced allocation;
- a reversal predates the allocation;
- the same allocation is reversed more than once;
- persisted financial state would project a negative balance.

All persistence queries are scoped by organization and Party/receivable identifiers before aggregation.

## Persistence

Migration `20260923033000_V1ReceivableBalanceFacts` introduces `v1_receivable_balance_facts` with exact `decimal(18,6)` amounts, business-date effectivity, immutable fact identity, receivable FK, reversal FK and indexes for Party-account reconstruction.

The migration is provider-neutral and must pass fresh-database migrations and integration tests on PostgreSQL and MySQL before merge.

This PR does not apply the migration to Neon production. Production migration remains a separate explicit approval gate.

## QA

Provider-real tests cover:

- organization isolation;
- Party isolation;
- currency isolation;
- open, partially settled and fully settled obligations;
- adjustment increase/decrease effects;
- collection allocation and reversal effects;
- prevention of over-settlement;
- duplicate reversal prevention;
- aging boundaries at 0/1/30/31/60/61/90/91 days.

Architecture tests lock the internal-only boundary and ensure no `account-summary`, receivables or collections HTTP surface is introduced by this increment.

## Remaining W2.4 prerequisite work

This increment intentionally does not make W2.4 implementation-ready. Remaining prerequisites are:

1. authoritative AP foundation for supplier obligations, adjustments, supplier-payment allocations and reversals;
2. Party role-aware composition of AR/AP with explicit completeness semantics;
3. field-level HTTP contract lock for `API-PTY-008`;
4. read-only HTTP implementation and production runtime acceptance.

Wave 2 remains `25/26` and global implemented-operation count remains `67/194`.
