# Blueprint Current State

Status: CURRENT HUMAN CHECKPOINT

Checkpoint date: 2026-09-12

Accepted functional baseline: `main@44478944bff3dbfee1e269347b48954015d8b3b9`
(merge of PR #77, `feat(fiscal): add durable daily report transport`).

Current pending governed increment: PR #78 `feat(fiscal): add BR same-SecEnvio correction lifecycle`.
PR #78 is not part of the accepted baseline until its exact final head is green and the human explicitly approves merge.

This file is the current human-readable checkpoint for the eFactura brownfield modernization. It does not replace requirements, architecture, API contracts or numbered implementation records. Files under `documentation/blueprint-brownfield/` remain historical inspection/remediation evidence and must not be rewritten to make the original AS-IS observations look current.

## Current validated baseline

- Runtime target: `.NET 10`.
- SDK pinned by `global.json`: `10.0.400`.
- Dedicated CI runner: `efactura-ci-01` on machine `Elena`.
- Current workflow selector: `[self-hosted, linux, x64, efactura-ci]`.
- CI database services: PostgreSQL 16 and MySQL 8.4 as isolated disposable service containers.
- NuGet vulnerability gate blocks known direct/transitive vulnerable packages.
- Deprecated/outdated package inventories remain advisory modernization evidence.
- Clean Architecture + Ports & Adapters remains mandatory for the modernization lineage.

The accepted `main` commit is the GitHub-verified merge of PR #77. Its post-merge Clean Architecture Guard #286, run `34695992604`, completed successfully.

## Accepted major v1 boundaries

The accepted baseline includes, among the earlier transactional and fiscal foundations:

1. Sales draft, validation and fiscal preview.
2. Inventory availability and controlled stock adjustment.
3. CAE authorization/allocation and atomic fiscal-number reservation.
4. Runtime/CI modernization to .NET 10 plus dependency/security gating.
5. Release-1 tax treatment, VAT/CFE eligibility and CFE 25.2 arithmetic foundations.
6. Sale confirmation and finance settlement foundations with atomic local confirmation effects.
7. Fiscal Document Identity and Organization Fiscal Issuer Profile foundations.
8. Immutable fiscal CFE content snapshots and frozen payment/unit-of-measure evidence.
9. Deterministic unsigned CFE generation for Release-1 101/111 and the bounded 102/103/112/113 domestic correction-note foundation.
10. Durable/replay-safe signing evidence, deterministic `TmstFirma`, XMLDSig signing, signed-artifact durability and pinned DGI FE XSD v1.44.2 validation.
11. Organization-scoped externally configured PFX composition with certificate identity pinning and ephemeral key loading.
12. DGI Testing readiness reconciliation that distinguishes free technical Testing from the formal traditional `Prueba de Testing`.
13. Reporte Diario v13.2 internal reconciliation with typed immutable source-fact evidence and lossless foreign-currency provenance.
14. Authoritative Reporte Diario v13.2 wire-contract evidence, pinned schema assets and deterministic monetary quantization.
15. Deterministic unsigned Reporte Diario XML generation.
16. Reporte Diario XMLDSig plus signed-XSD validation.
17. Durable signed Reporte Diario artifact persistence and byte-identical replay validation.
18. Durable Reporte Diario sequence/version lifecycle.
19. Durable DGI `EFACRECEPCIONREPORTE` transport with immediate `ACKRepDiario` AR/BR persistence.
20. Transport lifecycle `Prepared -> InFlight -> Received|Rejected|Unknown`, with no automatic retry from `Unknown`.
21. Provider-real PostgreSQL/MySQL serialization preventing duplicate network dispatch for the same durable submission.
22. Portable Reporte signing timestamp persistence as UTC instant plus original fiscal offset evidence.

Detailed bounded evidence remains under `documentation/blueprint-api-implementation/` through document `50_FISCAL_DAILY_REPORT_TRANSPORT.md` on the accepted baseline.

## Accepted fiscal transport boundary after PR #77

The accepted Reporte Diario path now reaches:

```text
immutable daily reconciliation evidence
-> deterministic Reporte Diario v13.2 unsigned XML
-> pinned schema validation
-> fiscal XMLDSig
-> signed-root validation
-> durable signed artifact
-> durable submission intent
-> SecEnvio ordering guard
-> Prepared
-> InFlight persisted before network
-> DGI EFACRECEPCIONREPORTE
-> immediate ACKRepDiario
-> Received(AR) | Rejected(BR) | Unknown
```

The accepted transport boundary preserves the exact signed artifact and never opportunistically rebuilds or resigns it for retry.

A submission in `Unknown` is never retried automatically because DGI may already have received the bytes. Later reconciliation must establish authoritative external state first.

The accepted PR #77 baseline intentionally stops at `BR`; it does not contain accepted same-`SecEnvio` corrected resubmission. That capability is the pending scope of PR #78.

## Pending PR #78 boundary, not yet accepted

PR #78 introduces a bounded local correction-revision lineage beneath the stable DGI identity:

`Organization + RUC + FechaResumen + SecEnvio`

The candidate preserves the original rejected root submission, signed artifact and ACK while creating new immutable correction revisions with independent signing/schema evidence and transport state.

The candidate also:

- parses typed `R01..R06` BR evidence behind an Infrastructure port;
- blocks `R05` same-sequence correction fail-closed;
- preserves operation-id replay without resigning;
- permits revision 2 -> revision 3 under the same `SecEnvio` only from durable authorizing BR evidence;
- keeps `Unknown` reconciliation-only;
- allows `SecEnvio N+1` only after either the root `N` or an accepted same-sequence correction for `N` has durable `AR` evidence;
- never rewrites the rejected root row into an accepted row;
- uses the root submission row as the transactional serialization anchor;
- adds provider-real persistence coverage for PostgreSQL and MySQL.

The detailed pending evidence is recorded in:
`documentation/blueprint-api-implementation/51_FISCAL_DAILY_REPORT_BR_SAME_SEQUENCE_CORRECTION.md`.

This section is descriptive of the open PR only. It does not promote PR #78 into the accepted baseline.

## DGI technical baseline currently used by the consumer

The governed fiscal lineage uses current official DGI artifacts already pinned/reviewed in the numbered implementation records, including:

- `Formato CFE v25.2`;
- `Formato_Sobre_v05`;
- `Formato Reporte CFE v13.2`;
- `Formato Mensajes Respuesta v19`;
- `XSDs_FE_V1.44.2`.

No XML element, namespace, mandatory-field rule, sequence rule, response state, Testing threshold, signature requirement or transport parameter may be inferred from memory, legacy demo code or provider examples when authoritative DGI evidence is required.

## DGI Testing readiness

The formal traditional `Prueba de Testing` remains blocked by product capabilities and external evidence. Local implementation progress must not be represented as DGI certification or Production readiness.

Current formal readiness remains exactly:

**BLOCKED BY MISSING PRODUCT CAPABILITIES**

External DGI Testing acceptance has not been established in this repository, and Production enablement remains separately gated.

## Explicitly not complete

The accepted baseline and pending PR #78 do not complete the following capabilities:

- operational correction-note command/API and automatic integration into the normal sale workflow;
- export CFE families and export-specific immutable evidence;
- contingency/CFC lifecycle;
- authoritative BCU quotation acquisition and automatic exchange-rate rule selection;
- automatic recovery semantics for Reporte Diario rejection `R05`;
- `EFACCONSULTARRESPUESTAREPORTE`;
- later Reporte Diario `DR` / `ER` / `FR` reconciliation lifecycle;
- automatic retry from ambiguous Reporte Diario `Unknown` state;
- independent cryptographic validation of DGI ACK signatures;
- Sobre v05 packaging/submission;
- authoritative external DGI Testing evidence;
- Production DGI/provider transport enablement and operational decision;
- OCSP/CRL and DGI-specific certificate habilitation verification;
- production HSM/Key Vault custody;
- remaining public fiscal/status/cancellation APIs;
- general receivable collection/payment allocation workflow;
- accounts payable/procurement/treasury/cash-management completion.

Regulatory decisions explicitly left open in accepted fiscal/tax documents remain open until separately reviewed against current official evidence.

## Blueprint evaluator checkpoint

Accepted historical eFactura evidence remains governed by its recorded evaluator versions and must not be rewritten retroactively.

Blueprint Master was last reverified for this lineage at `737556e24195aa909117790f2d7ff0be2fe0a474`, with root `VERSION = 0.5.2` and annotated tag `v0.5.2` resolving to that same commit.

There is no automatic consumer upgrade. Current consumer classification remains **DEFER formal 0.5.2 adoption** until its separate runtime/label migration items are deliberately executed and approved. The dedicated review remains in `documentation/BLUEPRINT_0_5_2_CONSUMER_COMPLIANCE_REVIEW.md`.

## Next bounded implementation sequence

PR #78 must close before later Reporte Diario response/reconciliation work advances.

After PR #78 is accepted, the next bounded sequence is expected to remain evidence-driven and may include:

1. authoritative consultation/reconciliation contract for `EFACCONSULTARRESPUESTAREPORTE`;
2. durable later-state handling for DGI `DR`, `ER` and `FR` without inventing supersession semantics;
3. separately evidenced `R05` sequence recovery;
4. Sobre v05 packaging/submission;
5. external DGI Testing evidence using legitimate credentials/certificate material supplied outside source control;
6. Production transport only after explicit technical and operational review.

The exact next slice must be revalidated against current authoritative DGI material before implementation.

## Known non-blocking modernization debt

Green builds may still report advisory legacy/modernization debt including deprecated/outdated dependencies, Application Insights legacy APIs, old ASP.NET abstractions, legacy provider/design packages, xUnit deprecation notices, nullable/analyzer warnings, obsolete cryptography APIs and Windows-only `System.Drawing` usage.

These items remain inventory for later bounded modernization slices and must not be upgraded wholesale without compatibility analysis.

## Repository governance at this checkpoint

- accepted `main`: `44478944bff3dbfee1e269347b48954015d8b3b9`;
- accepted merge: PR #77 `feat(fiscal): add durable daily report transport`;
- post-merge Clean Architecture Guard #286 (`34695992604`): SUCCESS;
- open governed increment: PR #78 `feat(fiscal): add BR same-SecEnvio correction lifecycle`;
- PR #78 remains pending until exact-head CI is green and human review is complete;
- one atomic slice per PR remains required;
- Blueprint 0.5.2 consumer adoption remains DEFER;
- merge requires final exact-head green CI and explicit human approval.
