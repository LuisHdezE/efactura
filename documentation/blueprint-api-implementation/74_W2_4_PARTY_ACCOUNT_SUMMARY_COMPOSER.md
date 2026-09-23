# W2.4 Party Account Summary application composer

Status: `IMPLEMENTED_PENDING_REVIEW`

Baseline: `main@eb8bb673925817473f7082076eefbe26a8252bd9`. This baseline contains the owner-approved PR #233 backend merge plus the later WebApp-only PR #232 and PR #235 changes from the parallel UI lane; those UI merges do not change W2.4 backend semantics.

Parent prerequisite contract: `documentation/api-completion-matrix/W2_4_PARTY_ACCOUNT_SUMMARY_PREREQUISITE_CONTRACT.md`.

This increment implements only the **Application Party account-summary composition** required by W2.4. It does not expose `API-PTY-008 getPartyAccountSummary` over HTTP and it does not change any public API completion count.

## 1. Implemented boundary

Application now owns `IPartyAccountSummaryReadModel` with the governed shape:

```text
GetAsync(organizationId, partyId, asOfUtc)
```

The implementation:

- loads the Party through the existing organization-scoped `IPartyRepository`;
- treats `CUSTOMER` as receivables applicability only;
- treats `SUPPLIER` as payables applicability only;
- never derives a balance from Party roles or Party master data;
- calls the authoritative AR read model only when the receivable side is applicable;
- calls the authoritative AP read model only when the payable side is applicable;
- requires both sides for a dual-role Party;
- preserves per-currency buckets and deterministic ordinal currency ordering;
- normalizes the requested `asOf` instant to UTC;
- fails closed if a downstream projection returns the wrong organization, Party or `asOf` instant;
- fails closed on duplicate/invalid currency buckets, negative amounts, and aging totals that do not reconcile with `outstanding` / `overdue`.

The composed Application result explicitly carries:

```text
organizationId
partyId
asOfUtc
receivablesApplicable
receivables[]
payablesApplicable
payables[]
```

The applicability flags are an internal Application distinction. They do not lock the eventual public HTTP DTO.

## 2. Provider-backed composition

The increment adds provider-real PostgreSQL/MySQL QA that creates a dual-role Party, persists a confirmed-sale receivable and a supplier payable, applies one collection allocation and one supplier-payment allocation, and reconstructs the final Party account summary through the real EF repositories plus the real AR/AP read models.

The provider-real test also proves that asking for the same Party through another organization scope returns `party.not_found` before any financial projection can leak data.

## 3. DI composition module

Infrastructure now exposes:

```text
AddW24PartyAccountSummaryComposition()
```

It registers the shared EF balance repositories, AR/AP source/effect ports, the two authoritative side read models and `IPartyAccountSummaryReadModel`.

This extension is deliberately **not wired into WebApi yet**. The next HTTP implementation increment must wire it only after the W2.4 field-level HTTP contract is locked. This keeps the current checkpoint free of public HTTP behavior.

## 4. QA gates

This checkpoint adds:

- customer-only composition behavior;
- supplier-only composition behavior;
- dual-role composition behavior;
- deterministic currency ordering;
- UTC `asOf` normalization;
- fail-closed Party-not-found / cross-organization behavior;
- fail-closed downstream scope mismatch;
- fail-closed duplicate currency behavior;
- fail-closed aging reconciliation (`current + overdue buckets == outstanding`, and overdue buckets sum to `overdue` at the governed 6-decimal precision);
- provider-real PostgreSQL composition;
- provider-real MySQL composition;
- architecture guards confirming that no W2.4 controller exists and Wave 2 counts remain unchanged.

Existing AR/AP foundation tests continue to own the zero/open/partial/full, adjustment, allocation, reversal and 0/1/30/31/60/61/90/91 aging-boundary matrices.

## 5. Deliberate non-scope

This increment does **not**:

- add a WebApi controller or route;
- finalize the field-level HTTP DTO;
- add or alter an OpenAPI operation;
- change authorization middleware or public permissions;
- add a database migration;
- apply AR or AP migrations to Neon production;
- mutate production data;
- deploy a new API revision;
- claim runtime acceptance;
- expose any Wave 3 public HTTP operation;
- advance Wave 2 or global implemented-operation counts.

## 6. Completion accounting

Until the public HTTP surface is separately contracted, implemented and runtime-accepted:

- Wave 2 remains `25 / 26` implemented HTTP surfaces;
- global public v1 remains `67 / 194` implemented;
- `API-PTY-008 getPartyAccountSummary` remains `MISSING_HTTP`.

With AR, AP and the provider-backed Application composer now represented in code, the next W2.4 frontier is **field-level HTTP contract lock** followed by the read-only HTTP implementation and runtime acceptance.

No production migration or runtime mutation is authorized by this checkpoint.
