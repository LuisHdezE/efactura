# Blueprint Current State

Status: CURRENT HUMAN CHECKPOINT

Checkpoint date: 2026-09-10

Accepted functional baseline: `main@44225493c60ee313e849f0f0d7628f7415744e49`
(merge of PR #66, `feat(fiscal): integrate daily report foreign-currency evidence`).

This file is the current human-readable checkpoint for the eFactura brownfield modernization.
It does not replace requirements, architecture, API-contract or numbered implementation records.
Files under `documentation/blueprint-brownfield/` remain historical inspection/remediation evidence
and must not be rewritten to make the original AS-IS observations look current.

## Current validated baseline

- Runtime target: `.NET 10`.
- SDK pinned by `global.json`: `10.0.400`.
- Dedicated CI runner: `efactura-ci-01` on machine `Elena`.
- Current workflow selector: `[self-hosted, linux, x64, efactura-ci]`.
- CI database services: PostgreSQL 16 and MySQL 8.4 as isolated disposable service containers.
- NuGet vulnerability gate blocks known direct/transitive vulnerable packages.
- Deprecated/outdated package inventories remain advisory modernization evidence.

Accepted fiscal/reporting lineage since the former PR #60 checkpoint:

- PR #61 `docs(fiscal): reconcile DGI Testing readiness gate`
  - merge commit: `808f0e70da6c2a39a3380bcb0068f5a66c6e754a`;
  - post-merge Clean Architecture Guard #221 (`34438269480`): SUCCESS.
- PR #62 `feat(fiscal): add domestic credit/debit note foundation`
  - merge commit: `af3e9c6c031a3954d51f0ee76045e899c1c22826`;
  - post-merge Clean Architecture Guard #228 (`34461958084`): SUCCESS.
- PR #63 `feat(fiscal): add daily report reconciliation foundation`
  - merge commit: `0b2110b8fe1b5ee3f92e917cb42c93146ddeeecf`;
  - post-merge Clean Architecture Guard #230 (`34538620978`): SUCCESS.
- PR #64 `docs(blueprint): reconcile current checkpoint after PR63`
  - approved head: `800cd3867f029f866603668a5e07d0eaf942f7d7`;
  - merge commit: `aa93257850d993b290bc6335104ef1844e3d1f08`;
  - post-merge Clean Architecture Guard #232 (`34543392012`): SUCCESS.
- PR #65 `feat(fiscal): add daily report source fact evidence`
  - approved head: `41888c4c2b4c82d39e97555be3e8a64386cb51ff`;
  - merge commit: `263fdbe6d1ec087529052113f59b15bd35313960`;
  - post-merge Clean Architecture Guard #237 (`34547264026`): SUCCESS.
- PR #66 `feat(fiscal): integrate daily report foreign-currency evidence`
  - approved head: `18cb5879ea08c9ca6773d6ea234c6ba296e3164b`;
  - merge commit / accepted baseline: `44225493c60ee313e849f0f0d7628f7415744e49`;
  - post-merge Clean Architecture Guard #241 (`34549610197`): SUCCESS.

The PR #66 post-merge guard passed:

- restore and dependency security gates: PASS;
- Release build: PASS;
- Clean Architecture guards: PASS;
- API v1 cross-cutting tests: PASS;
- legacy unit tests: PASS;
- PostgreSQL/MySQL transactional persistence integration tests: PASS.

The remaining package/analyzer warnings are advisory legacy/modernization debt and are not accepted as
security-gate failures.

## Accepted major v1 boundaries

Accepted slices now include:

1. Sales draft, validation and fiscal preview.
2. Inventory availability and controlled stock adjustment.
3. CAE authorization/allocation and atomic fiscal-number reservation.
4. Runtime/CI modernization to .NET 10 plus dependency/security gating.
5. Authoritative Release-1 tax treatment, VAT/CFE eligibility and CFE 25.2 arithmetic foundations.
6. Sale confirmation planning, settlement planning and Payment/Receivable persistence foundation.
7. Atomic local sale confirmation with tracked-stock and durable fiscalization effects.
8. Public `API-SAL-007 confirmSale`.
9. Fiscal Document Identity Foundation.
10. Organization Fiscal Issuer Profile Foundation.
11. Party address/contact contract completion needed by fiscal receiver evidence.
12. Immutable Fiscal CFE Content Snapshot Foundation.
13. Frozen CFE payment-form/settlement evidence.
14. Frozen DGI unit-of-measure evidence from catalog through SaleLine into immutable fiscal content.
15. Deterministic Unsigned CFE Builder for Release-1 e-Ticket (101) and e-Factura (111).
16. Durable/replay-safe signing-time evidence.
17. Deterministic `TmstFirma` signing payload.
18. XMLDSig fiscal signing adapter.
19. Durable signed CFE artifact persistence and replay validation.
20. Full signed-root validation against the pinned DGI FE XSD v1.44.2 set.
21. Organization-scoped externally configured PFX certificate composition with SHA-256 identity pinning and ephemeral key loading.
22. DGI Testing readiness reconciliation that distinguishes free technical Testing from the formal traditional `Prueba de Testing`.
23. Domestic 102/103/112/113 credit/debit-note foundation with immutable referenced-CFE evidence, deterministic unsigned XML, signing mapping and signed-root DGI XSD v1.44.2 validation.
24. Reporte Diario v13.2 internal reconciliation foundation with immutable document/annulment evidence, deterministic monetary grouping, explicit numbering consumption and fail-closed status/currency boundaries.
25. Typed immutable Reporte Diario source-fact evidence for exact signed-CFE identity, DGI AE/BE outcome, A-C19 presence/absence and foreign-currency conversion provenance.
26. Lossless foreign-currency integration into `FiscalDailyReportDocumentEvidence`, preserving original currency, UYU reporting currency, FX-evidence fingerprint and reliquidation marker while keeping deterministic reconciliation unchanged.

Detailed bounded evidence remains under `documentation/blueprint-api-implementation/`.

## Accepted fiscal boundary after PR #66

The accepted normal 101/111 sale flow reaches:

```text
Sale CONFIRMED
-> FiscalizationRequest PENDING
-> CAE number reserved
-> FiscalDocument IDENTITY_CREATED
-> immutable FiscalContentSnapshot
-> deterministic unsigned CFE
-> durable/replay-safe signing evidence
-> deterministic TmstFirma signing payload
-> organization-scoped certificate resolution
-> XMLDSig / ds:Signature
-> signed-root DGI XSD v1.44.2 validation
-> immutable signed artifact persistence
```

The accepted codebase also contains a bounded local correction-note chain for:

- 102 Nota de Crédito de e-Ticket;
- 103 Nota de Débito de e-Ticket;
- 112 Nota de Crédito de e-Factura;
- 113 Nota de Débito de e-Factura.

That correction-note foundation freezes referenced-CFE evidence into fiscal content, builds the note
deterministically, maps it through the existing signing pipeline and validates the signed root against
the pinned DGI FE XSD v1.44.2 set. It is not yet exposed through an operational note command/API or
connected automatically to the normal sale workflow.

The signing source is selected by `OrganizationId` from external configuration. PFX material is loaded
with `X509KeyStorageFlags.EphemeralKeySet`, must match a configured SHA-256 certificate fingerprint,
and is not committed to Git.

Replay never rebuilds a different signing timestamp or opportunistically resigns a stored fiscal
artifact. Persisted signed bytes, hashes, signing metadata and schema-validation evidence are checked
again before a replay result is returned.

### Accepted Reporte Diario internal boundary

The Reporte Diario v13.2 foundation can freeze and validate same-issuer daily evidence for domestic
101/102/103/111/112/113, support zero-operation days, aggregate monetary evidence by CFE type + document
date + DGI branch + `Pagos por cuenta de terceros`, reconstruct explicitly evidenced
used/emitted/annulled numbering and produce a deterministic SHA-256 reconciliation fingerprint.

PR #65 replaces unstructured caller assertions with typed immutable source facts where the current
model can preserve them losslessly:

- `FiscalDailyReportCfeIdentityEvidence` binds later source facts to one exact signed CFE and validates
  the signed artifact fiscal-content fingerprint against the immutable fiscal snapshot;
- `FiscalDailyReportDgiOutcomeEvidence` freezes exact current AE/BE semantics plus response-artifact
  fingerprint and supersession evidence, without pretending that a DGI transport/parser lifecycle
  already exists;
- `FiscalDailyReportThirdPartyPaymentEvidence` preserves A-C19 as exact field absence or exact value `1`,
  never arbitrary boolean coercion;
- `FiscalDailyReportCurrencyConversionEvidence` freezes source kind, exact decimal rate, source date,
  source fingerprint and reliquidation marker for non-UYU reporting.

PR #66 closes the lossless foreign-currency integration gap. `FiscalDailyReportDocumentEvidence` now
preserves:

- `OriginalCurrencyCode`;
- `ReportingCurrencyCode`, pinned to `UYU`;
- optional `CurrencyConversionEvidenceFingerprint`;
- `CurrencyConversionRequiresReliquidation`;
- all monetary partitions interpreted as reporting-currency amounts.

The accepted legacy `Capture(...)` boundary remains UYU-only and fail-closed. Foreign-currency
composition must pass through the typed conversion evidence. Converted monetary partitions are
multiplied by the exact frozen rate with no intermediate rounding, and mixed UYU + converted foreign-
currency CFE can be reconciled deterministically into the same UYU report bucket without losing FX
provenance.

The Reporte Diario foundation is still deliberately not a sendable DGI report. No accepted product
slice yet serializes, signs, persists or submits the Reporte Diario XML, acquires BCU quotations,
selects the applicable exchange-rate rule automatically, parses Mensaje de Respuesta v19, or maintains
an authoritative DGI response lifecycle.

No accepted product slice yet submits a CFE or Reporte Diario to DGI or interprets an authoritative
external DGI response end to end.

## DGI technical baseline currently used by the consumer

Official DGI publication was rechecked on 2026-09-10.
The `Documentos de interés` registry currently publishes for Testing and Production:

- `Formato CFE v25.2`;
- `Formato_Sobre_v05`;
- `Formato Reporte CFE v13 2`;
- `Formato Mensajes Respuesta v19`;
- `XSDs_FE_V1.44.2`.

Official registry:
`https://www.efactura.dgi.gub.uy/principal/ampliacion_de_contenido/documentos-de-interes?es=`

Official current ingress/testing instructive endpoint:
`https://www.efactura.dgi.gub.uy/files/instructivo-ingreso-al-regimen-cfe-archivo-pdf?es=`

Official FAQ endpoint:
`https://www.efactura.dgi.gub.uy/files/descargar-todas-las-preguntas-frecuentes?es=`

Official Reporte Diario v13.2 endpoint:
`https://www.efactura.dgi.gub.uy/files/formato_reporte_cfe_v13_2-pdf?es=`

No XML element, namespace, mandatory-field rule, Testing threshold, signature/validation ordering or
external status may be inferred from memory, legacy demo code or provider examples where current DGI
evidence is authoritative.

## DGI Testing readiness reconciliation

The DGI readiness review established that proving local signing with one e-Ticket and one e-Factura is
insufficient for the formal traditional `Prueba de Testing`.

Current DGI guidance distinguishes free technical Testing from the formal test required for the
traditional onboarding path.

For traditional onboarding, the formal test requires at least 50 distinct `Recibido` documents for
each member of the minimum CFE combo, all with the same issue/signature date:

- 101 e-Ticket;
- 102 Nota de Crédito de e-Ticket;
- 103 Nota de Débito de e-Ticket;
- 111 e-Factura;
- 112 Nota de Crédito de e-Factura;
- 113 Nota de Débito de e-Factura.

Rejected documents do not count toward those minima but must still be included in the corresponding
Reporte Diario. The Reporte Diario must reach `Recibido` and then be processed to `Reporte Procesado`.

The instructive also states that Testing is optional for the simplified ingress path and for certain
already-authorized issuers. The repository therefore must not assume the taxpayer onboarding mode
without explicit operational evidence.

Current readiness classification:

- deterministic local build/sign/XSD-validation foundation for 101/102/103/111/112/113: **IMPLEMENTED / LOCALLY VALIDATED**;
- operational correction-note API/workflow for 102/103/112/113: **NOT YET IMPLEMENTED**;
- internal Reporte Diario v13.2 reconciliation foundation: **IMPLEMENTED / LOCALLY VALIDATED**;
- typed Reporte Diario source facts for signed identity, AE/BE, A-C19 and FX provenance: **IMPLEMENTED / LOCALLY VALIDATED**;
- lossless non-UYU -> UYU Daily Report evidence composition: **IMPLEMENTED / LOCALLY VALIDATED**;
- automatic BCU quotation acquisition / exchange-rate rule selection: **NOT YET IMPLEMENTED**;
- authoritative DGI response parser/persistence/lifecycle: **NOT YET IMPLEMENTED**;
- sendable/signed/persisted Reporte Diario v13.2 artifact: **NOT YET IMPLEMENTED**;
- free external DGI Testing validation: **NOT YET EVIDENCED**;
- formal traditional `Prueba de Testing`: **BLOCKED BY MISSING PRODUCT CAPABILITIES**;
- Production transport/readiness: **OUT OF SCOPE AND NOT EVIDENCED**.

The detailed readiness reconciliation is recorded in:
`documentation/blueprint-api-implementation/36_DGI_TESTING_READINESS_RECONCILIATION.md`.

The accepted domestic-note foundation is recorded in:
`documentation/blueprint-api-implementation/37_FISCAL_DOMESTIC_CREDIT_DEBIT_NOTE_FOUNDATION.md`.

The accepted Daily Report reconciliation foundation is recorded in:
`documentation/blueprint-api-implementation/38_FISCAL_DAILY_REPORT_RECONCILIATION_FOUNDATION.md`.

The accepted Daily Report source-fact evidence is recorded in:
`documentation/blueprint-api-implementation/39_FISCAL_DAILY_REPORT_SOURCE_FACT_EVIDENCE.md`.

The accepted foreign-currency integration is recorded in:
`documentation/blueprint-api-implementation/40_FISCAL_DAILY_REPORT_FX_INTEGRATION.md`.

## Explicitly not complete

The following remain outside the accepted current product baseline:

- operational correction-note command/API and integration into the normal application workflow;
- note-level `IndGlobal` synthesis/global-reference workflow;
- export CFE families and export-specific immutable evidence;
- contingency/CFC lifecycle;
- automatic extraction/freeze of CFE A-C19 from the normal fiscal-content/build pipeline;
- authoritative BCU exchange-rate acquisition and business-day quotation lookup;
- automatic selection/arbitration of the applicable fiscal exchange-rate rule;
- automatic A-C110/A-C111 extraction from CFE XML where required by future wire work;
- authoritative DGI response persistence, supersession selection and lifecycle;
- Reporte Diario v13.2 XML serializer and pinned authoritative report XSD/signature package;
- wire-level Reporte Diario decimal formatting/rounding rules;
- Reporte Diario advanced signature;
- Reporte Diario persistence/replay/sequence lifecycle;
- automatic Reporte Diario scheduling and next-business-day send orchestration;
- DGI Sobre v05 packaging/submission;
- DGI Mensaje de Respuesta v19 parsing and status interpretation;
- authoritative Testing acceptance evidence;
- DGI/provider Production transport;
- direct-DGI-vs-provider Production decision;
- external acceptance/rejection retry lifecycle;
- OCSP/CRL status verification;
- DGI-specific certificate habilitation verification;
- production HSM/Key Vault custody;
- `API-SAL-008 cancelSale`;
- `API-SAL-009 getSaleFiscalizationStatus`;
- public `API-FIS-*` document routes;
- general receivable collection/payment allocation workflow;
- accounts payable/procurement/treasury/cash-management completion.

Regulatory decisions explicitly left open in accepted fiscal/tax documents remain open until
separately reviewed against current official evidence.

## Blueprint evaluator checkpoint

Accepted historical eFactura evidence remains governed by:

- Blueprint evaluator version: `0.5.1`;
- exact evaluator commit: `ac8be4e3332b13cab7d27f12e6a62d5d60e9ff4e`;
- annotated evaluator tag: `v0.5.1`.

Blueprint Master was last reverified for this lineage at
`737556e24195aa909117790f2d7ff0be2fe0a474`, with root `VERSION = 0.5.2` and annotated tag `v0.5.2`
resolving to that same commit.

There is no automatic consumer upgrade. Current consumer classification remains **DEFER formal 0.5.2
adoption** until its separate runtime/label migration items are deliberately executed and approved.
The dedicated review remains in `documentation/BLUEPRINT_0_5_2_CONSUMER_COMPLIANCE_REVIEW.md`.

Historical files identifying evaluator 0.5.1 remain historical evidence and must not be rewritten
merely to display the newer Master version.

## Reconciled next bounded implementation sequence

The source-fact and foreign-currency evidence steps planned after PR #63 are now accepted through PR #66.
The next bounded sequence is:

1. **Pin the authoritative Reporte Diario v13.2 wire contract**
   - obtain the official current report-format package and any authoritative XSD/signature assets;
   - preserve the authoritative assets in-repository with provenance and hashes where licensing/publication permits;
   - document namespace, root/child ordering, cardinalities, decimal representation, required/conditional fields and signature requirements from authoritative material only;
   - keep all uncertain or externally dependent rules fail-closed rather than inferred.
2. **Deterministic Reporte Diario artifact generation and validation**
   - serialize only from immutable reconciliation evidence;
   - apply authoritative wire-level formatting/rounding;
   - sign and validate without rereading mutable fiscal masters.
3. **Reporte Diario persistence/replay/sequence lifecycle**
   - persist immutable report bytes/hash/signing evidence;
   - preserve correction/reliquidation sequence semantics and replay the same artifact rather than rebuilding it opportunistically.
4. **Testing package/submission contract**
   - evidence Sobre v05 and Mensaje de Respuesta v19;
   - choose an isolated Testing submission mechanism or operator-assisted export path;
   - keep Production transport separately gated.
5. **External DGI Testing evidence**
   - execute only with legitimate credentials/certificate material supplied outside source control;
   - record authoritative DGI receipt/status evidence without manufacturing success.
6. **Production transport and operational lifecycle**
   - only after the required external evidence and transport contract are separately reviewed and accepted.

If the actual taxpayer uses the simplified onboarding path, the formal 50-per-type test may not be
mandatory, but that operational fact must be established explicitly rather than inferred by code.

## Known non-blocking modernization debt

Current green builds still report legacy/advisory debt including deprecated/outdated dependencies,
Application Insights legacy APIs, `Microsoft.AspNetCore.Http.Abstractions 2.2.0`, legacy Npgsql
extension/design packages, xUnit 2.x deprecation notices, nullable/analyzer warnings, obsolete
cryptography APIs and Windows-only `System.Drawing` usage.

GitHub Actions also reports Node 20 deprecation warnings for action versions currently being forced to
execute on Node 24.

These items remain inventory for later bounded modernization slices and must not be upgraded wholesale
without compatibility analysis.

## Repository governance at this checkpoint

- PR #64 is merged at `aa93257850d993b290bc6335104ef1844e3d1f08`; post-merge Guard #232: SUCCESS.
- PR #65 is merged at `263fdbe6d1ec087529052113f59b15bd35313960`; post-merge Guard #237: SUCCESS.
- PR #66 is merged at `44225493c60ee313e849f0f0d7628f7415744e49`; post-merge Guard #241: SUCCESS.
- accepted `main`: `44225493c60ee313e849f0f0d7628f7415744e49`.
- no open PRs existed immediately before the current checkpoint-reconciliation branch was created.
- Blueprint 0.5.2 consumer adoption remains DEFER.
- one atomic slice per PR remains required.
- merge requires final exact-head green CI and explicit human approval.
