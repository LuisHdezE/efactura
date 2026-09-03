# API Implementation 18 — Sale Confirmation Planning Foundation

Status: IMPLEMENTED_ON_BRANCH / PENDING_HUMAN_ACCEPTANCE

Branch: `blueprint/sale-confirmation-planning`

## Purpose

Create the deterministic, side-effect-free planning boundary that the future contracted `API-SAL-007 confirmSale` transaction must consume before it is allowed to mutate commercial, fiscal, inventory or financial state.

The previous slice established authoritative CFE 25.2 arithmetic. This slice binds that arithmetic and the complete server-side CFE selection provenance to a validated sale confirmation plan while deliberately stopping before the irreversible transaction boundary.

## Why this is a separate slice

The accepted public `SaleConfirmRequest` includes payment intents and/or credit terms. The Release-1 v1 implementation does not yet have the complete Payment/Receivable command surface required to honor that contract honestly.

Therefore this slice does **not** expose `POST /api/v1/sales/{saleId}/confirm` yet. Exposing the endpoint without its accepted financial semantics would incorrectly mark `API-SAL-007` implemented.

## Added Application boundary

`src/Application/Sales/SaleConfirmationPlanning.cs` adds:

- `SaleConfirmationPlanningLine`;
- `SaleConfirmationPlanningRequest`;
- `SaleConfirmationPlan`;
- `SaleConfirmationPlanner`.

The planner is pure Application orchestration over the already accepted Domain fiscal arithmetic and selection results. It does not depend on EF Core, ASP.NET, Infrastructure, PostgreSQL/MySQL or an external fiscal provider.

## Required planning evidence

A confirmation plan is created only when:

- the sale state supplied to planning is `VALIDATED`;
- sale ID/version are present;
- the validation fingerprint is present;
- server-side `CfeSelectionResult` is final (`Selected`);
- the selected family corresponds to exactly one eligible candidate and receiver-identification requirement;
- CFE selection format is the same supported 25.2 format used by arithmetic;
- CFE selection carries non-empty regulatory `RuleEvidence` effective on the sale fiscal date;
- the selection has no unresolved missing facts;
- every fiscal line has authoritative resolved `TaxRateResolution` evidence accepted by `CfeArithmeticCalculator`;
- inventory availability is `Ready`;
- inventory evidence contains exactly one row per product item requirement;
- the grouped product quantity in inventory evidence exactly matches the grouped product quantity in the confirmation plan;
- every stock-tracked product has sufficient quantity, authoritative available quantity and `PositionVersion`.

The planner fails closed on stale/incomplete fiscal-selection, tax-rate or inventory evidence.

## Authoritative fiscal result and selection provenance

The plan preserves the complete accepted `CfeSelectionResult`, including:

- selected CFE family;
- receiver-identification requirement;
- eligible candidate set;
- decision reasons;
- regulatory rule evidence;
- CFE format version.

It also executes `CfeArithmeticCalculator` with `UruguayCfe25_2ArithmeticCatalog.Current`.

The resulting plan therefore carries:

- server-selected CFE decision provenance;
- CFE format version;
- arithmetic rule-pack version;
- item amounts;
- minimum/basic/export buckets;
- VAT totals calculated from header taxable buckets;
- final total;
- tax-rate rule-pack provenance per line;
- regulatory evidence inherited by both selection and fiscal calculation.

No preview tax total or bare client-supplied CFE code is promoted to issuance authority.

## Confirmation fingerprint

A deterministic SHA-256 `ConfirmationFingerprint` is derived from material confirmation evidence including:

- sale ID/version;
- validation fingerprint;
- selected CFE family and receiver-identification requirement;
- CFE selection format and effective rule-evidence identity/version/range;
- fiscal date/currency;
- CFE arithmetic format/rule-pack;
- authoritative net/VAT/total amounts;
- per-line fiscal amount/rate/rule-pack evidence identity;
- grouped inventory requirement, available quantity and `PositionVersion`.

This fingerprint is designed to become part of the later transactional confirmation evidence. Any material change to validated fiscal-selection, arithmetic or inventory inputs changes the fingerprint.

## Inventory boundary

This slice preserves inventory expectations but **does not mutate inventory**.

That is deliberate. The next transactional slice must consume the stored/validated position versions and create an explicit sale-sourced immutable `StockMovement` in the same local transaction as sale confirmation and fiscal-number/snapshot effects.

The existing manual `ApplyAdjustment` path is not reused as a hidden sale-consumption command.

## Verification added

`CrossCuttingTests/SaleConfirmationPlanningTests.cs` proves:

- authoritative header-bucket VAT is used in the confirmation plan;
- complete CFE selection provenance is retained;
- inventory position version is retained;
- DRAFT sales fail closed;
- grouped product quantities must match inventory evidence exactly;
- tracked inventory requires authoritative position version;
- confirmation fingerprint changes when inventory version changes;
- unresolved tax rates fail before any confirmation effects exist;
- a selected CFE without regulatory provenance fails closed.

`ArchitectureTests/SaleConfirmationPlanningArchitectureTests.cs` proves:

- planning remains Application-owned and provider/framework free;
- CFE 25.2 arithmetic is consumed as authority;
- complete CFE selection provenance is required and retained;
- inventory evidence is preserved without stock mutation;
- no CAE reservation, unit-of-work, outbox, Payment or Receivable behavior leaks into this planning slice;
- public `/confirm` remains absent.

## Explicit non-goals

Not implemented by this slice:

- `API-SAL-007` HTTP endpoint;
- Sale state transition to `CONFIRMED`;
- CAE number reservation;
- FiscalDocument persistence;
- issuer/receiver fiscal snapshot persistence;
- stock consumption/movement persistence;
- payment creation;
- receivable creation/credit terms;
- cash-shift effects;
- XML generation or XSD validation;
- signature/certificate custody;
- DGI/provider transport;
- fiscal acceptance/rejection/regularization.

## Next accepted implementation step

After human acceptance, the next slice should create the transactional confirmation foundation that atomically coordinates:

1. validated confirmation-plan recheck, including CFE selection/arithmetic provenance;
2. sale `VALIDATED -> CONFIRMED` transition;
3. stock-tracked product consumption with optimistic version protection and immutable sale movement;
4. CAE reservation through `IFiscalNumberAllocator`;
5. immutable FiscalDocument issuance snapshot in a pre-generation/pending state;
6. audit + outbox + idempotency completion;
7. rollback of every staged effect on any local failure.

Payment/Receivable semantics must be introduced before the public `API-SAL-007` contract is finally exposed.
