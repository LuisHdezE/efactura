# API Implementation 12 — Sales Draft and Fiscal Preview

Status: MERGED / VERIFIED_IN_MAIN

Merged PR: #29 `feat(sales): add draft and fiscal preview v1 slice`

## Purpose

Introduce the bounded Release-1 Sales preparation surface without crossing into sale confirmation, fiscal issuance, payment effects or inventory mutation.

## Accepted API surface

The slice is limited to API-SAL-001..006:

- `GET /api/v1/sales`;
- `POST /api/v1/sales`;
- `GET /api/v1/sales/{saleId}`;
- `PATCH /api/v1/sales/{saleId}`;
- `POST /api/v1/sales/{saleId}/validate`;
- `GET /api/v1/sales/{saleId}/fiscal-preview`.

## Architectural boundary

- Sales Domain remains independent from Taxation, Fiscal, EF Core and ASP.NET.
- Application orchestrates TaxTreatment -> TaxRate -> CFE eligibility -> CFE selection preparation -> fiscal preview.
- Public clients cannot assert authoritative export confirmation or Article-34 qualification booleans.
- Public enums use the accepted UPPER_SNAKE_CASE contract; numeric and ambiguous public enum inputs are rejected.
- Draft writes, durable audit, outbox and idempotency evidence share one local transaction.
- Editing a validated draft returns it to DRAFT and invalidates the prior validation fingerprint.
- Product-bearing sales consume authoritative Inventory availability through an inward contract rather than owning stock state.

## Fiscal-preview constraints

Fiscal preview is preparation evidence, not issuance. Preview arithmetic remains non-authoritative until the final CFE arithmetic/rounding slice is accepted. CFE selection cannot override an eligibility result that requires review.

Transaction currency remains ISO alpha-3. `UYI` can represent a transaction already expressed in Unidad Indexada; conversion from UYU/USD to UI remains outside this slice and requires an authoritative effective-date quotation source.

## Verification at acceptance

The PR was validated with Release build, Clean Architecture guards, API v1 cross-cutting tests and PostgreSQL/MySQL transactional persistence. The persistence suite at that stage completed 63/63 PASS, including rollback evidence proving Sale + SaleLines + Audit + Outbox + Idempotency are atomic on both providers.

The later current-main checkpoint supersedes those historical counts and records the consolidated 170/170 baseline.

## Explicit non-goals

Not implemented by this slice:

- sale confirmation;
- sale cancellation/final fiscalization lifecycle;
- payment or receivable effects;
- stock consumption;
- CAE/fiscal number reservation;
- CFE XML generation/signing/transport.
