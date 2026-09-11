# Fiscal Daily Report Source Fact Evidence

Status: IMPLEMENTATION CANDIDATE

Candidate branch: `blueprint/fiscal-daily-report-source-facts`

Base accepted main: `aa93257850d993b290bc6335104ef1844e3d1f08`

## Purpose

PR #63 established deterministic internal reconciliation for Reporte Diario v13.2, but three inputs were still represented only by caller-supplied primitive values/fingerprints:

1. whether a CFE belongs to the current DGI received/not-rejected population;
2. whether the CFE carries A-C19 `Indicador Pagos por cuenta de terceros`;
3. which authoritative rate/source/date justifies converting a non-UYU CFE into UYU for Reporte Diario.

This slice freezes those source facts as typed immutable evidence before the wire-format/persistence/transport work begins.

It does **not** manufacture a DGI lifecycle that the product does not yet have.

## Official DGI evidence rechecked on 2026-09-10

Current DGI `Documentos de interés` publishes:

- `Formato CFE v25.2` for Testing and Production;
- `Formato Reporte CFE v13 2` for Testing and Production;
- `Formato Mensajes Respuesta v19` for Testing and Production.

Registry:

`https://www.efactura.dgi.gub.uy/principal/ampliacion_de_contenido/documentos-de-interes?es=`

### A-C19

Current `Formato CFE v25.2` defines A-C19 as `Indicador Pagos por cuenta de terceros`:

- value `1` means pagos por cuenta de terceros;
- the field is conditional for the accepted ticket/factura and correction-note families.

Current DGI FAQ 4.36 also states that Reporte Diario must identify those amounts using the corresponding third-party-payment indicator.

The domain therefore preserves **absence vs exact value 1**. It does not coerce arbitrary numeric input to a boolean.

### DGI CFE reception outcome

Current `Formato Mensajes Respuesta v19`, field 26 `Estado Recepción del Comprobante`, defines:

- `AE` = Comprobante Recibido;
- `BE` = Comprobante Rechazado (CFE).

A later DGI response may supersede an earlier known response, including rejection after an earlier received state. This slice therefore stores an immutable supersession fingerprint and requires callers to provide the current authoritative outcome when composing Daily Report evidence.

No parser, polling loop or DGI transport is invented here.

### Foreign-currency Reporte Diario amounts

Current DGI FAQ 9.6 states that Reporte Diario amounts are expressed in UYU and, from 2022-11-01, non-UYU CFE amounts use fiscal exchange-rate rules. The FAQ establishes, among other rules:

- use the BCU quotation with all decimal digits for the fiscal exchange rate;
- foreign currency generally uses the interbank buyer-banknote closing quotation from the day before the operation;
- if that quotation does not exist, use the last previous business-day quotation;
- if no quotation exists for the transaction currency, the CFE exchange rate may be used;
- domestic correction notes use the exchange rate carried by the correction CFE for Reporte Diario purposes;
- a future-dated CFE fallback may require later Reporte Diario reliquidation.

This slice freezes the chosen source kind, exact decimal rate, source date, source-evidence fingerprint and reliquidation marker. It does not round the rate or converted amount at the source-evidence layer.

For `BcuFiscalQuotation`, the source date must be a real date strictly before the CFE fiscal date. This does not attempt to calculate business days inside Domain; acquisition/selection of the correct BCU quotation remains an external source responsibility.

## Implemented domain evidence

### `FiscalDailyReportCfeIdentityEvidence`

Binds every later source fact to the exact signed CFE through:

- `FiscalDocumentId`;
- `SignedArtifactId`;
- `SigningEvidenceId`;
- organization and issuer RUC;
- CFE type / series / number;
- fiscal date;
- currency;
- CFE advanced-signature timestamp;
- signed artifact fiscal-content fingerprint;
- signed-content SHA-256;
- deterministic evidence fingerprint.

Capture requires the signed artifact fiscal-content fingerprint explicitly and verifies that it equals the immutable `FiscalContentSnapshot.ContentFingerprint`. Merely naming a `SignedArtifactId` is insufficient.

The identity is revalidated against `FiscalDocument` + `FiscalContentSnapshot` before composition.

### `FiscalDailyReportDgiOutcomeEvidence`

Freezes:

- exact CFE identity fingerprint;
- CFE signing timestamp;
- typed outcome `Received` / `Rejected`;
- exact DGI v19 code `AE` / `BE`;
- observation timestamp;
- source response-artifact SHA-256;
- optional superseded-evidence fingerprint;
- deterministic evidence fingerprint.

`AE` may enter the emitted/not-rejected composition boundary only when supplied as the current authoritative outcome.

`BE` can be converted into explicit `FiscalDailyReportAnnulmentEvidence` of kind `DgiRejection` without inferring a gap.

### `FiscalDailyReportThirdPartyPaymentEvidence`

Freezes the exact A-C19 source shape:

- `null` = field absent;
- `1` = pagos por cuenta de terceros;
- any other numeric value = fail closed.

The source artifact fingerprint and deterministic evidence fingerprint are both preserved.

### `FiscalDailyReportCurrencyConversionEvidence`

For non-UYU domestic accepted CFE it freezes:

- CFE identity fingerprint;
- CFE family and fiscal date;
- original currency;
- typed rate source;
- exact decimal rate to UYU;
- non-default rate source date;
- source-evidence SHA-256;
- whether later reliquidation may be required;
- deterministic evidence fingerprint.

Accepted source kinds in this bounded model:

1. `BcuFiscalQuotation`;
2. `CfeRateNoBcuQuotation`;
3. `CorrectionCfeRate`;
4. `FutureDateCfeRate`.

Domestic 102/103/112/113 require `CorrectionCfeRate` in this evidence layer.

A future-date fallback requires the reliquidation marker.

A BCU quotation source date cannot be the CFE date or a later date because the published rule requires the previous-day quotation, or an earlier previous business day when necessary.

## Composition now enabled

`FiscalDailyReportSourceFactComposer` replaces raw primitive caller evidence where the current PR #63 model can preserve the facts losslessly:

- UYU + current `AE` + explicit A-C19 evidence -> `FiscalDailyReportDocumentEvidence`;
- current `BE` -> explicit DGI-rejection `FiscalDailyReportAnnulmentEvidence`.

The composed evidence keeps the typed source-evidence fingerprints, so the previous generic fingerprint boundary is no longer an unstructured assertion for these UYU cases.

## Deliberate foreign-currency fail-closed boundary

Although currency-conversion source facts can now be frozen and validated, this slice does **not** yet compose a foreign-currency CFE into `FiscalDailyReportDocumentEvidence`.

Reason: the current PR #63 record has no dedicated field for the currency-conversion evidence fingerprint. Converting amounts now and silently dropping that provenance would break the project's immutable-evidence rule.

Therefore composition fails with:

`fiscal.daily_report.source_facts.foreign_currency_integration_required`

until the reconciliation document-evidence shape is extended to preserve:

- original currency;
- conversion-evidence fingerprint;
- converted UYU monetary partitions.

This is intentional, not a missing test workaround.

## Tests added

`FiscalDailyReportSourceFactsTests` covers:

- deterministic signed-CFE source identity;
- signed-artifact/snapshot fingerprint mismatch rejection;
- AE/BE mapping;
- immutable supersession chain;
- A-C19 absent vs exact value `1`;
- rejection of invalid A-C19 values;
- UYU composition from typed status + A-C19 evidence;
- BE rejection excluded from emitted population;
- BE -> explicit annulment composition;
- exact-decimal BCU conversion calculation;
- non-default and strictly previous BCU source-date rule;
- correction-note exchange-rate source rule;
- future-date reliquidation marker;
- foreign-currency composition remains fail closed;
- JSON roundtrip/integrity and tamper detection.

## Explicit exclusions

This PR does not implement:

- DGI Mensaje v19 parser;
- DGI HTTP transport;
- authoritative DGI response persistence/lifecycle;
- automatic latest-response selection;
- automatic A-C19 capture from Sale/FiscalContentSnapshot;
- A-C19 XML generation in the normal CFE builder;
- BCU quotation acquisition;
- business-day calendar/quotation lookup;
- UI/UR support in the current three-letter currency model;
- foreign-currency integration into final Daily Report monetary evidence;
- Reporte Diario v13.2 XML serializer;
- Reporte Diario XSD/signature package;
- Reporte Diario persistence/replay/sequence lifecycle;
- Sobre v05;
- Production transport;
- Blueprint Master or `.blueprint/` changes.

## Readiness classification

Formal traditional DGI Testing readiness remains exactly:

**BLOCKED BY MISSING PRODUCT CAPABILITIES**

This source-fact foundation improves evidence quality but does not make the product externally ready.

## Next bounded step

Before pinning and implementing the Reporte Diario v13.2 wire artifact, extend `FiscalDailyReportDocumentEvidence` and deterministic reconciliation so a non-UYU CFE can preserve the frozen conversion evidence fingerprint and converted UYU monetary partitions without losing provenance.

Only after that source-fact integration is reviewed should the implementation proceed to the authoritative v13.2 XML/XSD/signature contract.
