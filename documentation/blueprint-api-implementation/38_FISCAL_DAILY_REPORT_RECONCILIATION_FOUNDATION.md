# Fiscal Daily Report Reconciliation Foundation

Status: IMPLEMENTATION CANDIDATE  
Consumer: `LuisHdezE/efactura`  
Baseline at slice start: `main@af3e9c6c031a3954d51f0ee76045e899c1c22826`  
Branch: `blueprint/fiscal-daily-report-reconciliation-foundation`  
DGI Reporte Diario format baseline: `v13.2`

## Purpose

This slice establishes the immutable and deterministic reconciliation evidence needed before the
product can safely generate a DGI Reporte Diario.

It does **not** claim that a sendable Reporte Diario XML exists. The repository does not yet pin a
Reporte Diario v13.2 XSD/serializer/signature package comparable to the accepted CFE
`DgiFeV1_44_2` schema set. This slice therefore stops before XML generation instead of guessing the
wire contract.

The formal DGI Testing readiness classification remains:

**BLOCKED BY MISSING PRODUCT CAPABILITIES**

## Official DGI evidence revalidated on 2026-09-10

The DGI `Documentos de interés` registry continues to publish `Formato Reporte CFE v13 2` for
Testing and Production.

Authoritative sources used for this bounded foundation:

- DGI Documentos de interés:
  `https://www.efactura.dgi.gub.uy/principal/ampliacion_de_contenido/documentos-de-interes?es=`
- DGI Formato Reporte CFE v13.2:
  `https://www.efactura.dgi.gub.uy/files/formato_reporte_cfe_v13_2-pdf?es=`
- DGI Preguntas Frecuentes:
  `https://www.efactura.dgi.gub.uy/files/descargar-todas-las-preguntas-frecuentes?es=`

The current official material establishes the following rules used here:

1. One Reporte Diario is required for every calendar day, including days with no operations.
2. The report consolidates CFE/CFC used between `00:00:00` and `23:59:59`.
3. Monetary consolidation is discriminated by CFE/CFC type, document date, DGI branch and the
   `Pagos por cuenta de terceros` indicator.
4. The report also contains CFE numbering consumption, including emitted and annulled numbers.
5. The report summary date is the day being reported, identified as the advanced-signature date of
   the reported CFE; later corrections increase the report sequence and DGI uses the highest
   sequence for that calendar day.
6. Report amounts are expressed in UYU. Since 2022-11-01, foreign-currency amounts use the fiscal
   exchange-rate rules documented by DGI, including BCU quotation rules rather than blindly copying
   an arbitrary local conversion.
7. In v13.2, `Cantidad de CFE utilizados` includes CFE received by DGI, CFE annulled because of DGI
   rejection and numbers annulled by the company.
8. `Cantidad de CFE anulados` includes DGI-rejected CFE and company annulments. A CFE corrected by a
   Nota de Crédito is not an annulled number for this purpose.
9. `Cantidad de CFE emitidos` refers to CFE emitted and not rejected by DGI.
10. Used-number ranges and annulled-number ranges are reported separately; current v13.2 allows up
    to 50,000 range repetitions.

## Implemented bounded model

### `FiscalDailyReportDocumentEvidence`

Freezes one CFE that is eligible to contribute monetary totals to the report:

- `FiscalDocumentId`
- `SignedArtifactId`
- `SigningEvidenceId`
- organization and issuer RUC
- CFE type, series and number
- fiscal document date
- DGI branch code
- explicit `PaymentOnBehalfOfThirdParty`
- fingerprint of the evidence that establishes that third-party-payment indicator
- advanced-signature timestamp
- fiscal-content fingerprint
- signed-content hash
- **reporting-status evidence fingerprint**
- current Release-1 UYU monetary partitions
- deterministic evidence SHA-256 fingerprint

The reporting-status fingerprint is deliberate. A signed CFE alone does not prove that the CFE is
in DGI v13.2 field C29's "emitted and not rejected" population. The future DGI-response lifecycle
must provide authoritative status evidence before a final report can classify a document that way.

The third-party-payment evidence fingerprint is also deliberate. The current accepted fiscal
snapshot does not yet freeze CFE field A-C19. This model therefore refuses to turn absence of that
capability into an implicit `false` business fact.

### `FiscalDailyReportAnnulmentEvidence`

Represents an explicitly evidenced annulled fiscal number.

Supported reasons in this foundation:

- company annulment;
- DGI rejection.

DGI rejection evidence must identify the rejected `FiscalDocument` and its signed artifact.
Company annulments may exist without an emitted/signed CFE identity.

No numbering gap is converted into an annulment. A gap remains a gap until explicit durable source
evidence exists.

### `FiscalDailyReportSnapshot`

Creates an immutable daily reconciliation for:

- one organization;
- one issuer RUC;
- one summary date;
- one positive report sequence;
- format baseline `13.2`.

It deterministically derives:

- used-CFE count;
- monetary summary buckets by CFE type + fiscal date + DGI branch + third-party-payment indicator;
- per-CFE-type used, emitted and annulled counts;
- contiguous used-number ranges by series;
- contiguous annulled-number ranges by series;
- one SHA-256 reconciliation fingerprint.

A zero-operation day is valid and produces empty summary/consumption collections.

## Current Release-1 support

This slice accepts only the already bounded domestic families:

- `101` e-Ticket;
- `102` Nota de Crédito de e-Ticket;
- `103` Nota de Débito de e-Ticket;
- `111` e-Factura;
- `112` Nota de Crédito de e-Factura;
- `113` Nota de Débito de e-Factura.

`121` and all other export/contingency families remain fail-closed.

## Foreign-currency boundary

The current immutable CFE snapshot preserves the transaction currency but does not yet preserve the
complete fiscal exchange-rate evidence needed to reconstruct DGI's current Reporte Diario conversion
rules.

Therefore this foundation deliberately rejects non-UYU document evidence with:

`fiscal.daily_report.foreign_currency_conversion_evidence_required`

This is stricter than inventing a conversion from the CFE total or from mutable exchange-rate state.
A later bounded slice must freeze the authoritative fiscal exchange-rate source/date/value evidence
before foreign-currency CFE can enter Reporte Diario monetary aggregation.

## Rejection/annulment boundary

DGI v13.2 distinguishes:

- CFE used;
- CFE annulled, including DGI rejections and company annulments;
- CFE emitted and not rejected.

The current product has no accepted DGI transport/response lifecycle, so it cannot yet manufacture
the final status classification. The domain foundation accepts an external status-evidence
fingerprint but does not invent the outcome.

This is also why the presence of a signed artifact does not automatically increment the final
"emitted and not rejected" count without explicit reporting-status evidence.

## Deliberately excluded

This PR does not add:

- Reporte Diario v13.2 XML serialization;
- a pinned Reporte Diario XSD set;
- advanced electronic signature of the report;
- Reporte Diario persistence or lifecycle state;
- automatic scheduling inside the first 18 hours of the next business day;
- DGI `Recibido` / `Reporte Procesado` response handling;
- DGI rejection parsing;
- automatic BCU fiscal exchange-rate acquisition;
- automatic A-C19 `Pagos por cuenta de terceros` capture from the current sale/CFE flow;
- CFC/contingency reporting;
- export CFE reporting;
- Sobre v05 packaging;
- Mensaje de Respuesta v19 parsing;
- DGI Testing HTTP transport;
- Production transport;
- credentials, private keys or PFX material;
- Blueprint Master changes;
- `.blueprint/` adoption.

## Test evidence in this slice

Cross-cutting tests cover:

- a required zero-operation calendar-day snapshot;
- all six accepted domestic CFE families;
- deterministic monetary grouping;
- explicit third-party-payment grouping;
- used-range reconstruction across emitted and explicitly annulled numbers;
- no annulment inference from numbering gaps;
- DGI-rejection evidence requirements;
- emitted/annulled identity overlap rejection;
- foreign-currency fail-closed behavior;
- summary-date/advanced-signature-date consistency;
- signed-artifact/content-fingerprint consistency;
- export-family fail-closed behavior;
- deterministic reconciliation regardless of input order;
- JSON round-trip integrity;
- tamper detection through the reconciliation SHA-256 fingerprint.

## Human-checkpoint drift observed before this slice

`documentation/BLUEPRINT_CURRENT_STATE.md` on the accepted PR #62 baseline still describes PR #60 as
the accepted functional baseline and still lists 102/103/112/113 as incomplete. That checkpoint is
therefore stale relative to Git history.

This feature PR does not rewrite that checkpoint as though the new Reporte Diario foundation were
already accepted. The checkpoint must be reconciled in a separate closure/governance change after
the actual feature merge result and post-merge CI are known.

## Next bounded step after this foundation

After this foundation is accepted, the next Reporte Diario work should be split rather than jumped
directly to transport:

1. freeze the missing source facts required for final report classification and currency conversion;
2. pin the authoritative Reporte Diario v13.2 XML/XSD/signature contract in-repository;
3. implement deterministic report artifact generation and validation;
4. persist/replay the report artifact and its sequence;
5. only then connect the Reporte to the separately gated Testing package/submission contract.

External DGI acceptance remains evidence that must be obtained from DGI. It cannot be simulated by
unit tests or documentation.
