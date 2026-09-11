# Fiscal Daily Report Foreign-Currency Integration

Status: IMPLEMENTATION CANDIDATE

Candidate branch: `blueprint/fiscal-daily-report-fx-integration`

Base accepted main: `263fdbe6d1ec087529052113f59b15bd35313960`

## Purpose

PR #65 froze authoritative source facts for Reporte Diario conversion but deliberately stopped before inserting converted foreign-currency CFE into `FiscalDailyReportDocumentEvidence`.

This slice closes that bounded gap without weakening the existing UYU-only capture contract.

The goal is lossless internal reconciliation evidence:

- preserve the CFE original currency;
- preserve that Reporte Diario monetary partitions are expressed in UYU;
- preserve the exact immutable FX-evidence fingerprint used for conversion;
- preserve whether the chosen FX rule may require later Reporte Diario reliquidation;
- convert every monetary partition using the frozen rate with no intermediate rounding;
- keep the existing grouping and range-reconciliation algorithm unchanged.

## Official DGI evidence rechecked on 2026-09-10

Authoritative sources:

- DGI `Formato Reporte CFE v13.2`:
  `https://www.efactura.dgi.gub.uy/files/formato_reporte_cfe_v13_2-pdf?es=`
- DGI current FAQ download / question 9.6:
  `https://www.efactura.dgi.gub.uy/files/descargar-todas-las-preguntas-frecuentes?es=`

The current DGI material states that:

1. Reporte Diario monetary information is expressed in pesos uruguayos.
2. Since 2022-11-01, CFE not issued in national currency must be included converted using the fiscal exchange-rate rules.
3. BCU quotation must be used with all decimal digits when that rule applies.
4. The general foreign-currency rule uses the interbank buyer-banknote closing quotation from the day before the operation, or the last previous business day if no quotation exists on that date.
5. If no quotation exists for the transaction currency, the CFE exchange rate may be used.
6. Domestic correction notes use the exchange rate carried by the correction CFE for Reporte Diario purposes.
7. Future-dated CFE may use the rate carried by the CFE and can require later Reporte Diario reliquidation.

This slice consumes the typed source evidence introduced by PR #65. It does not acquire BCU rates or decide which rule applies at runtime.

## Domain changes

### `FiscalDailyReportDocumentEvidence`

The record now preserves:

- `OriginalCurrencyCode`;
- `ReportingCurrencyCode`, pinned to `UYU`;
- optional `CurrencyConversionEvidenceFingerprint`;
- `CurrencyConversionRequiresReliquidation`;
- all existing monetary partitions, now explicitly interpreted as reporting-currency amounts.

The evidence fingerprint includes all four new provenance fields.

### Existing `Capture(...)` remains UYU-only

The accepted public capture boundary continues to fail closed for non-UYU CFE with:

`fiscal.daily_report.foreign_currency_conversion_evidence_required`

This preserves the already accepted contract and prevents callers from supplying a naked conversion rate.

For UYU evidence:

- original currency = `UYU`;
- reporting currency = `UYU`;
- FX fingerprint = `null`;
- reliquidation marker = `false`.

Integrity rejects any UYU evidence carrying foreign-currency provenance.

### Typed converted capture

An internal typed path accepts only:

- the exact `FiscalDocument`;
- its immutable `FiscalContentSnapshot`;
- the frozen signed-CFE identity from PR #65;
- the frozen `FiscalDailyReportCurrencyConversionEvidence`;
- existing DGI-status and A-C19 evidence fingerprints.

The path verifies that the document, snapshot, signed identity and conversion evidence all refer to the same CFE before conversion.

Every monetary partition is multiplied by the exact frozen `RateToUyu`:

- net;
- non-taxed;
- minimum-rate taxable;
- basic-rate taxable;
- export;
- minimum VAT;
- basic VAT;
- total VAT;
- total amount.

No rounding is applied in this internal evidence layer. Wire-format precision and rounding remain part of the later authoritative Reporte Diario v13.2 serializer contract.

### `FiscalDailyReportForeignCurrencyComposer`

A new explicit composition boundary combines:

- signed-CFE identity;
- current authoritative `AE` DGI outcome;
- A-C19 source evidence;
- frozen FX conversion evidence;

and emits `FiscalDailyReportDocumentEvidence` whose monetary fields are all in UYU while retaining foreign-currency provenance.

The pre-existing `FiscalDailyReportSourceFactComposer` remains unchanged and UYU-oriented. Its former foreign-currency fail-closed behavior is intentionally retained as a compatibility/safety boundary rather than silently broadening its semantics.

## Deterministic reconciliation behavior

`FiscalDailyReportSnapshot` does not need a new grouping algorithm.

Once every document evidence contains reporting amounts in UYU, the existing deterministic grouping by:

- CFE type;
- fiscal date;
- DGI branch;
- A-C19 third-party-payment indicator;

continues to produce the required monetary buckets.

This also means UYU and converted foreign-currency CFE can coexist in one bucket without mixing currencies internally.

## Tests added

`FiscalDailyReportForeignCurrencyIntegrationTests` covers:

- exact-decimal conversion of every monetary partition;
- preservation of original currency and reporting currency;
- preservation of FX evidence fingerprint;
- UYU + USD aggregation into one UYU summary bucket;
- propagation of future-date reliquidation marker;
- semantic rejection if FX provenance is stripped even after recomputing the outer evidence fingerprint;
- JSON roundtrip/integrity for converted document evidence.

Existing PR #63/#65 tests continue to prove that the legacy UYU path remains deterministic and that the old generic source-fact composer does not silently accept foreign currency.

## Explicit exclusions

This PR does not implement:

- BCU quotation acquisition;
- business-day calendar lookup;
- arbitration for currencies not quoted directly;
- automatic selection of the applicable FX rule;
- automatic A-C110/A-C111 extraction from CFE XML;
- UI/UR support beyond the current three-letter currency model;
- Reporte Diario v13.2 XML serialization;
- wire-level decimal formatting/rounding rules;
- Reporte Diario XSD/signature package;
- Reporte Diario persistence/replay/sequence lifecycle;
- DGI Mensaje v19 parser/transport lifecycle;
- Sobre v05;
- Production transport;
- Blueprint Master or `.blueprint/` changes.

## Readiness classification

Formal traditional DGI Testing readiness remains exactly:

**BLOCKED BY MISSING PRODUCT CAPABILITIES**

## Next bounded step

After review of this slice, the next major technical boundary is to pin the authoritative Reporte Diario v13.2 wire contract and its XSD/signature assets with provenance, then implement deterministic XML generation and validation from the now-complete internal reconciliation evidence.
