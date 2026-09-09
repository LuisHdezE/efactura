# 30 - Fiscal Unit of Measure Evidence Prerequisite

Status: **CANDIDATE / NOT ACCEPTED UNTIL MERGE**

Date: 2026-09-09

Accepted predecessor baseline: `main@d343f76f4911df8599daeb8ab43f138a35fa553a`
(merge of PR #52, CFE build/sign/validate lifecycle reconciliation).

Blueprint evaluator for this consumer remains:
`0.5.1@ac8be4e3332b13cab7d27f12e6a62d5d60e9ff4e`.

## Goal

Close one evidence gap discovered while preparing the deterministic **Unsigned CFE Builder**.

The active DGI **Formato CFE v25.2** defines the detail-line unit-of-measure field as mandatory for the relevant CFE sale families and defines it as an `ALFA4` field with a maximum length of four characters. The format explicitly permits `N/A` when a unit does not conceptually apply, but that value is still fiscal content and therefore must not be manufactured by the XML builder.

Before this slice:

- the canonical `CommercialItem` already owned a required `Unit` value;
- confirmed `SaleLine` preserved item code, name, kind, quantity and price, but dropped the unit;
- `FiscalContentSnapshot` therefore had no immutable source for the future DGI detail-line unit;
- rereading `CommercialItem` during XML BUILD would violate the accepted immutable-snapshot boundary and could observe a unit changed after the sale was created.

This slice preserves the existing catalog fact instead of inventing an XML default.

## Evidence flow

The accepted flow becomes:

`CommercialItem.Unit`
`-> SaleDraftBuilder`
`-> SaleLine.UnitOfMeasure`
`-> v1_sale_lines.UnitOfMeasure`
`-> FiscalContentLineSnapshot.UnitOfMeasure`
`-> future Unsigned CFE Builder`

The builder itself remains outside this slice.

## Catalog versus DGI constraints

The application catalog remains allowed to store its existing commercial unit text up to the accepted 40-character limit. This slice does not reinterpret the catalog as a DGI code table.

At fiscal snapshot creation, the application requires the already-frozen sale-line unit to fit the current DGI CFE detail field:

- missing or blank unit: `fiscal.snapshot.item_unit_missing`;
- more than four characters: `fiscal.snapshot.item_unit_not_dgi_compatible`;
- no truncation;
- no automatic replacement with `N/A`;
- no mutable catalog reread.

A catalog value explicitly stored as `N/A` is preserved like any other accepted source fact.

## Historical compatibility

The new `v1_sale_lines.UnitOfMeasure` column is nullable and receives no data backfill.

That is intentional. A pre-slice sale did not preserve its sale-time commercial unit, and copying the current catalog value into an old row would manufacture historical evidence.

Therefore:

- historical sale rows remain readable;
- historical rows with no unit cannot create a new fiscal content snapshot;
- the failure is a missing-prerequisite conflict, not an implicit reconstruction;
- new sales created through the canonical `SaleDraftBuilder` freeze `CommercialItem.Unit` immediately.

Existing serialized `FiscalContentSnapshot` payloads also remain readable. `FiscalContentLineSnapshot.UnitOfMeasure` is nullable only for backward compatibility, and the content fingerprint preserves the legacy algorithm when that field is absent. New snapshots produced by the factory always contain the unit and include it in the fingerprint material.

## Determinism and replay

For new fiscal content snapshots:

- the unit comes only from the immutable confirmed `SaleLine`;
- the factory normalizes the frozen value to uppercase for the DGI-facing snapshot;
- the unit participates in the content SHA-256 fingerprint;
- changing the unit after snapshot creation changes the fingerprint and fails integrity checks;
- replay reads the stored snapshot and does not revisit catalog data.

## Persistence

Provider-neutral persistence adds one nullable column:

`v1_sale_lines.UnitOfMeasure` with maximum length 40.

The same mapping is used for PostgreSQL and MySQL. No provider-specific data type or backfill is introduced.

## Automated proof

The candidate proves, on the supported persistence providers, that:

- a unit frozen on the sale line survives sale persistence and reaches the immutable fiscal snapshot;
- the snapshot round-trip retains the unit together with the existing issuer, receiver and fiscal calculation evidence;
- a historical sale without unit evidence fails closed;
- a sale unit longer than the current four-character DGI field fails without truncation;
- old direct snapshot instances that do not contain the new optional field retain their legacy fingerprint semantics;
- existing snapshot replay and rollback behavior remains unchanged.

Exact-head CI evidence is recorded in PR #53 before review or merge approval. This implementation note intentionally does not embed a moving candidate head.

## Explicit non-scope

This slice does not implement:

- CFE XML construction;
- `UniMed` XML serialization;
- item-code-type mapping;
- `IndFact` mapping;
- `TmstFirma`;
- XML digital signature;
- certificate/private-key custody;
- full DGI XSD validation;
- signed artifact persistence;
- DGI/provider transport.

## Next boundary after acceptance

After this prerequisite is merged and exact-head CI is accepted, work may return to the bounded **Unsigned CFE Builder**.

That builder must consume `FiscalDocument` identity plus immutable `FiscalContentSnapshot` evidence only. It must not reread `CommercialItem`, Company, FiscalLocation, Party or TaxProfile master data and must not create a unit-of-measure default that is absent from accepted evidence.
