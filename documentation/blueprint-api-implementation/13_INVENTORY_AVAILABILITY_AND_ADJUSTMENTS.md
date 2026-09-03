# API Implementation 13 — Inventory Availability and Stock Adjustment

Status: MERGED / VERIFIED_IN_MAIN

Merged PR: #30 `feat(inventory): add availability and stock adjustment v1`

## Purpose

Establish the authoritative Release-1 inventory availability boundary needed by Sales while keeping stock mutation narrowly limited to approved adjustments.

## Accepted API surface

The slice is limited to API-INV-001..004:

- `GET /api/v1/inventory/positions`;
- `GET /api/v1/inventory/positions/{positionId}`;
- `GET /api/v1/inventory/movements`;
- `POST /api/v1/inventory/adjustments`.

## Core invariants

- `InventoryPosition` owns current quantity for organization + item + location and carries an application-managed version.
- The database enforces a unique authoritative position for `(OrganizationId, ItemId, LocationId)`.
- `StockMovement` is append-oriented immutable business evidence and records the resulting position version for replay/auditability.
- Stock adjustment requires explicit `inventory.adjust` permission, organization/location scope, `Idempotency-Key`, reason and expected version.
- Position + movement + durable audit + outbox + idempotency commit in one local transaction.
- Repositories do not own `SaveChanges`, BEGIN/COMMIT/ROLLBACK or ad-hoc SQL mutation.

## Sales integration boundary

Sales consumes Inventory only through `IInventoryAvailabilityChecker`. Sales does not own or mutate stock state.

Catalog `TrackInventory` is authoritative: non-stock-tracked products do not block Sales availability. Missing/inactive catalog items fail availability closed. The Sales validation fingerprint includes the availability/version snapshot used by the preview.

## Concurrency and replay

A concurrent first-position creation race is translated to the portable API conflict contract only for the exact authoritative unique-position guard on PostgreSQL/MySQL. Unrelated persistence failures are not silently reclassified.

Idempotent replay returns the original successful adjustment quantity/version snapshot even if later movements changed the current position.

## Verification at acceptance

The accepted PR passed Release build, Clean Architecture guards, API v1 cross-cutting tests and PostgreSQL/MySQL integration. The persistence suite at that stage completed 77/77 PASS, including atomic success/rollback, stale-version rejection, availability aggregation, `TrackInventory` behavior and duplicate-position concurrency cases on both providers.

The later current-main checkpoint supersedes those historical counts and records the consolidated 170/170 baseline.

## Explicit non-goals

Not implemented by this slice:

- stock transfers;
- replenishment;
- procurement/goods receipt;
- sale confirmation or stock consumption;
- arbitrary adjustment modes beyond the accepted `quantityDelta` contract;
- an invented negative-stock policy.
