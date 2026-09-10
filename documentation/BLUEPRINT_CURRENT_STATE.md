# Blueprint Current State

Status: CURRENT HUMAN CHECKPOINT

Checkpoint date: 2026-09-10

Accepted functional baseline: `main@0b2110b8fe1b5ee3f92e917cb42c93146ddeeecf`
(merge of PR #63, `feat(fiscal): add daily report reconciliation foundation`).

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

The accepted lineage immediately after the former PR #60 checkpoint is:

- PR #61 `docs(fiscal): reconcile DGI Testing readiness gate`
  - merge commit: `808f0e70da6c2a39a3380bcb0068f5a66c6e754a`;
  - post-merge Clean Architecture Guard #221 (`34438269480`): SUCCESS.
- PR #62 `feat(fiscal): add domestic credit/debit note foundation`
  - merge commit: `af3e9c6c031a3954d51f0ee76045e899c1c22826`;
  - post-merge Clean Architecture Guard #228 (`34461958084`): SUCCESS.
- PR #63 `feat(fiscal): add daily report reconciliation foundation`
  - approved feature head: `60106136c771b4178dafd0eb793ad9b9ec2ad2a3`;
  - merge commit / accepted baseline: `0b2110b8fe1b5ee3f92e917cb42c93146ddeeecf`;
  - post-merge Clean Architecture Guard #230 (`34538620978`): SUCCESS.

The PR #63 post-merge guard passed:

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
24. Reporte Diario v13.2 internal reconciliation foundation with immutable document/annulment evidence, deterministic monetary grouping, explicit numbering consumption and fail-closed currency/status boundaries.

Detailed bounded evidence remains under `documentation/blueprint-api-implementation/`.

## Accepted fiscal boundary after PR #63

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

PR #63 additionally accepts the internal Reporte Diario reconciliation foundation. It can freeze and
validate same-issuer daily evidence for domestic 101/102/103/111/112/113, support zero-operation days,
aggregate monetary evidence by CFE type + document date + DGI branch + `Pagos por cuenta de terceros`,
reconstruct explicitly evidenced used/emitted/annulled numbering and produce a deterministic SHA-256
reconciliation fingerprint.

The Reporte Diario foundation is deliberately not a sendable DGI report. It currently requires
explicit external fingerprints for reporting-status and third-party-payment evidence, rejects
non-UYU monetary evidence until authoritative fiscal exchange-rate evidence is frozen, never infers
annulments from numbering gaps and does not serialize/sign/persist/submit the Reporte Diario XML.

No accepted product slice yet submits a CFE or Reporte Diario to DGI or interprets an authoritative
DGI response.

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

## Explicitly not complete

The following remain outside the accepted current product baseline:

- operational correction-note command/API and integration into the normal application workflow;
- note-level `IndGlobal` synthesis/global-reference workflow;
- export CFE families and export-specific immutable evidence;
- contingency/CFC lifecycle;
- automatic capture/freeze of CFE A-C19 `Pagos por cuenta de terceros` evidence;
- authoritative fiscal/BCU exchange-rate acquisition and immutable conversion evidence for Reporte Diario;
- authoritative DGI reporting-status/rejection evidence lifecycle;
- Reporte Diario v13.2 XML serializer and pinned report XSD/signature package;
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

The next sequence after accepted PR #63 is:

1. **Freeze missing Reporte Diario source facts**
   - connect the currently explicit reporting-status evidence boundary to authoritative durable DGI outcome evidence when that lifecycle exists;
   - freeze A-C19 `Pagos por cuenta de terceros` source evidence instead of defaulting absence to false;
   - freeze authoritative fiscal/BCU exchange-rate source/date/value evidence required for non-UYU report amounts;
   - keep unsupported export/CFC paths fail-closed.
2. **Pin the authoritative Reporte Diario v13.2 wire contract**
   - preserve the official XML/XSD/signature package in-repository with provenance;
   - document namespace, ordering, required fields and signature rules from authoritative material only.
3. **Deterministic Reporte Diario artifact generation and validation**
   - serialize from immutable reconciliation evidence;
   - sign and validate without rereading mutable fiscal masters.
4. **Reporte Diario persistence/replay/sequence lifecycle**
   - persist immutable report bytes/hash/signing evidence;
   - preserve correction sequence semantics and replay the same artifact rather than rebuilding it opportunistically.
5. **Testing package/submission contract**
   - evidence Sobre v05 and Mensaje de Respuesta v19;
   - choose an isolated Testing submission mechanism or operator-assisted export path;
   - keep Production transport separately gated.
6. **External DGI Testing evidence**
   - execute only with legitimate credentials/certificate material supplied outside source control;
   - record authoritative DGI receipt/status evidence without manufacturing success.
7. **Production transport and operational lifecycle**
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

- PR #61 is merged at `808f0e70da6c2a39a3380bcb0068f5a66c6e754a`; post-merge Guard #221: SUCCESS.
- PR #62 is merged at `af3e9c6c031a3954d51f0ee76045e899c1c22826`; post-merge Guard #228: SUCCESS.
- PR #63 is merged at `0b2110b8fe1b5ee3f92e917cb42c93146ddeeecf`; post-merge Guard #230: SUCCESS.
- accepted `main`: `0b2110b8fe1b5ee3f92e917cb42c93146ddeeecf`.
- no open PRs existed immediately before the current checkpoint-reconciliation branch was created.
- Blueprint 0.5.2 consumer adoption remains DEFER.
- one atomic slice per PR remains required.
- merge requires final exact-head green CI and explicit human approval.
