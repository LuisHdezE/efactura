# Blueprint Current State

Status: CURRENT HUMAN CHECKPOINT

Checkpoint date: 2026-09-10

Accepted functional baseline: `main@dd379ea000a5cf55673659b74c241901bf3e52db`
(merge of PR #60, `feat(fiscal): compose organization-scoped PFX signing identity`).

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

PR #60 exact-head `Clean Architecture Guard` run #218 (`34427603949`) completed successfully before merge.
Post-merge `Clean Architecture Guard` run #219 (`34429049916`) completed successfully on accepted
`main@dd379ea000a5cf55673659b74c241901bf3e52db`, including:

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

Detailed bounded evidence remains under `documentation/blueprint-api-implementation/`.

## Accepted fiscal boundary after PR #60

The accepted product flow now reaches:

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

Release-1 signed-CFE support currently covers:

- 101 e-Ticket;
- 111 e-Factura.

The signing source is selected by `OrganizationId` from external configuration. PFX material is loaded
with `X509KeyStorageFlags.EphemeralKeySet`, must match a configured SHA-256 certificate fingerprint,
and is not committed to Git.

Replay never rebuilds a different signing timestamp or opportunistically resigns a stored fiscal
artifact. Persisted signed bytes, hashes, signing metadata and schema-validation evidence are checked
again before a replay result is returned.

No accepted product slice yet submits a CFE to DGI or interprets an authoritative DGI response.

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

The post-PR #60 review found that the earlier shorthand idea of proving DGI Testing with only one
signed e-Ticket and one signed e-Factura is insufficient for the formal traditional `Prueba de Testing`.

The current DGI instructive distinguishes free technical Testing from the formal test required for the
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

- local signed-CFE readiness for 101/111: **READY**;
- free external DGI Testing validation: **NOT YET EVIDENCED**;
- formal traditional `Prueba de Testing`: **BLOCKED BY MISSING PRODUCT CAPABILITIES**;
- Production transport/readiness: **OUT OF SCOPE AND NOT EVIDENCED**.

The detailed reconciliation is recorded in:
`documentation/blueprint-api-implementation/36_DGI_TESTING_READINESS_RECONCILIATION.md`.

## Explicitly not complete

The following remain outside the accepted current product baseline:

- 102 Nota de Crédito de e-Ticket;
- 103 Nota de Débito de e-Ticket;
- 112 Nota de Crédito de e-Factura;
- 113 Nota de Débito de e-Factura;
- export CFE families and export-specific immutable evidence;
- contingency/CFC lifecycle;
- Reporte Diario generation/lifecycle;
- DGI Sobre packaging/submission;
- DGI Mensaje de Respuesta parsing and status interpretation;
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

The next sequence after the PR #60 signing composition is:

1. **Domestic credit/debit note foundation**
   - implement the official correction/reference semantics needed for 102/103/112/113;
   - freeze reference/correction evidence so later mutable state cannot rewrite a note;
   - keep export and contingency families fail-closed.
2. **Daily Report foundation**
   - implement the currently published Reporte Diario v13.2 contract and durable reconciliation evidence.
3. **Testing package/submission contract**
   - evidence Sobre v05 and Mensaje de Respuesta v19;
   - choose an isolated Testing submission mechanism or operator-assisted export path;
   - keep Production transport separately gated.
4. **External DGI Testing evidence**
   - execute only with legitimate credentials/certificate material supplied outside source control;
   - record authoritative DGI receipt/status evidence without manufacturing success.
5. **Production transport and operational lifecycle**
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

- PR #60 is merged.
- accepted `main`: `dd379ea000a5cf55673659b74c241901bf3e52db`.
- PR #60 exact-head Guard #218: SUCCESS.
- post-merge Guard #219 on accepted main: SUCCESS.
- no open PRs existed immediately before this reconciliation branch was created.
- Blueprint 0.5.2 consumer adoption remains DEFER.
- one atomic slice per PR remains required.
- merge requires final exact-head green CI and explicit human approval.
