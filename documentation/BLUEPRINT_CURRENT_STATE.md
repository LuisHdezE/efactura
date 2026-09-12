# Blueprint Current State

Status: CURRENT HUMAN CHECKPOINT

Checkpoint date: 2026-09-12

Accepted functional baseline: `main@0846d63b02db2cff65bc3c86c4eca4cffa20b409`
(merge of PR #78, `feat(fiscal): add BR same-SecEnvio correction lifecycle`).

Current pending governed increment: PR #79 `feat(fiscal): add daily report response consultation foundation`.
PR #79 is not part of the accepted baseline until its exact final head is green and the human explicitly approves merge.

This file is the current human-readable checkpoint for the eFactura brownfield modernization. It does not replace requirements, architecture, API contracts or numbered implementation records. Files under `documentation/blueprint-brownfield/` remain historical inspection/remediation evidence and must not be rewritten to make the original AS-IS observations look current.

## Current validated baseline

- Runtime target: `.NET 10`.
- SDK pinned by `global.json`: `10.0.400`.
- Dedicated CI runner: `efactura-ci-01` on machine `Elena`.
- Current workflow selector: `[self-hosted, linux, x64, efactura-ci]`.
- CI database services: PostgreSQL 16 and MySQL 8.4 as isolated disposable service containers.
- NuGet vulnerability gate blocks known direct/transitive vulnerable packages.
- Clean Architecture + Ports & Adapters remains mandatory.

The accepted `main` commit is the GitHub-verified merge of PR #78. Its post-merge Clean Architecture Guard #301, run `34722941078`, completed successfully for both Build/Architecture and PostgreSQL/MySQL transaction jobs.

## Accepted major v1 boundaries

The accepted baseline includes, among the earlier transactional and fiscal foundations:

1. Sales draft, validation and fiscal preview.
2. Inventory availability and controlled stock adjustment.
3. CAE authorization/allocation and atomic fiscal-number reservation.
4. Runtime/CI modernization to .NET 10 plus dependency/security gating.
5. Release-1 tax treatment, VAT/CFE eligibility and CFE 25.2 arithmetic foundations.
6. Sale confirmation and finance settlement foundations with atomic local effects.
7. Fiscal Document Identity and Organization Fiscal Issuer Profile foundations.
8. Immutable CFE content snapshots and frozen payment/unit-of-measure evidence.
9. Deterministic unsigned CFE generation for Release-1 101/111 and bounded 102/103/112/113 domestic correction-note foundation.
10. Durable/replay-safe signing evidence, deterministic `TmstFirma`, XMLDSig signing, signed-artifact durability and pinned DGI FE XSD v1.44.2 validation.
11. Organization-scoped externally configured PFX composition with ephemeral key loading.
12. DGI Testing readiness reconciliation that distinguishes technical Testing from the formal traditional `Prueba de Testing`.
13. Reporte Diario v13.2 internal reconciliation with typed immutable source-fact evidence and lossless FX provenance.
14. Authoritative Reporte Diario v13.2 wire contract, pinned schema assets and deterministic monetary quantization.
15. Deterministic unsigned Reporte Diario XML generation, XMLDSig, signed-XSD validation and durable signed artifact replay.
16. Durable Reporte Diario sequence/version lifecycle.
17. Durable DGI `EFACRECEPCIONREPORTE` transport with immediate `ACKRepDiario` AR/BR persistence.
18. Transport lifecycle `Prepared -> InFlight -> Received|Rejected|Unknown`, with no automatic retry from `Unknown`.
19. Provider-real PostgreSQL/MySQL serialization preventing duplicate network dispatch.
20. Portable Reporte signing timestamp persistence as UTC instant plus original fiscal offset evidence.
21. Same-`SecEnvio` BR correction lineage with immutable local revisions and independent signing/schema evidence.
22. Typed `R01..R06` BR reason evidence with `R05` fail-closed for separate sequence reconciliation.
23. Operation-id replay for BR correction without re-signing.
24. Root submission row as transactional serialization anchor for correction dispatch.
25. `SecEnvio N+1` authorization after durable AR on either the root N submission or an accepted same-`SecEnvio` correction for N.
26. Original rejected root submission/artifact/ACK remain immutable and are never rewritten into AR.
27. Provider-real PostgreSQL/MySQL correction persistence, local-revision uniqueness and Uruguay signing-offset round-trip.

Detailed bounded evidence remains under `documentation/blueprint-api-implementation/` through document `51_FISCAL_DAILY_REPORT_BR_SAME_SEQUENCE_CORRECTION.md` on the accepted baseline.

## Accepted Reporte Diario boundary after PR #78

The accepted Reporte Diario path now reaches:

```text
immutable daily reconciliation evidence
-> deterministic Reporte Diario v13.2 unsigned XML
-> pinned schema validation
-> fiscal XMLDSig
-> durable signed artifact
-> durable submission intent
-> SecEnvio ordering guard
-> Prepared
-> InFlight persisted before network
-> DGI EFACRECEPCIONREPORTE
-> immediate ACKRepDiario
-> Received(AR) | Rejected(BR) | Unknown
-> ordinary BR evidence
-> immutable local same-SecEnvio correction revision
-> new signing/schema evidence
-> Prepared -> InFlight -> Received(AR) | Rejected(BR) | Unknown
```

For an ordinary BR, corrected resubmission preserves the DGI `SecEnvio` while creating new immutable local correction evidence. `R05` cannot authorize this path and remains fail-closed.

`Unknown` is never automatically retried because DGI may already have received the bytes. Authoritative external evidence must be established before any later reconciliation or resend decision.

## Pending PR #79 boundary, not yet accepted

PR #79 introduces a bounded authoritative consultation foundation for a **known durable DGI `IdReceptor`** using current official DGI `ws_consultas / EFACCONSULTARRESPUESTAREPORTE` evidence.

The candidate:

- resolves the receiver id only from durable root-submission or BR-correction evidence;
- queries DGI through an Infrastructure-only WS-Security adapter;
- keeps endpoint and SOAPAction as mandatory external configuration;
- validates the returned original `ACKRepDiario`, matching `IDReceptor` and immediate `AR`/`BR` state;
- stores the raw consulted ACK, SHA-256 hash, timestamp and local-consistency classification append-only;
- makes consultation replay-safe through organization-scoped `OperationId`;
- never rewrites root or correction transport state automatically;
- does not claim that this method alone can reconcile an `Unknown` attempt with no known `IdReceptor`.

The detailed pending evidence is recorded in:
`documentation/blueprint-api-implementation/52_FISCAL_DAILY_REPORT_RESPONSE_CONSULTATION.md`.

This section describes the open candidate only. It does not promote PR #79 into the accepted baseline.

## DGI technical baseline currently used by the consumer

The governed fiscal lineage uses official DGI artifacts already pinned/reviewed in the numbered implementation records, including:

- `Formato CFE v25.2`;
- `Formato_Sobre_v05`;
- `Formato Reporte CFE v13.2`;
- `Formato Mensajes Respuesta v19`;
- `XSDs_FE_V1.44.2`;
- `Servicios Web Externos DGI`, code `T-5.020.00.001-000005`, version `1.9`, dated `13/05/2024`, for the pending consultation boundary.

No XML element, namespace, mandatory-field rule, sequence rule, response state, Testing threshold, signature requirement, endpoint or SOAPAction may be inferred from memory, legacy demo code or provider examples when authoritative DGI evidence is required.

## DGI Testing readiness

The formal traditional `Prueba de Testing` remains blocked by product capabilities and external evidence. Local implementation progress must not be represented as DGI certification or Production readiness.

Current formal readiness remains exactly:

**BLOCKED BY MISSING PRODUCT CAPABILITIES**

External DGI Testing acceptance has not been established in this repository, and Production enablement remains separately gated.

## Explicitly not complete

The accepted baseline and pending PR #79 do not complete:

- operational correction-note command/API integration into the normal sale workflow;
- export CFE families and export-specific immutable evidence;
- contingency/CFC lifecycle;
- authoritative BCU quotation acquisition and automatic exchange-rate rule selection;
- automatic recovery semantics for Reporte Diario rejection `R05`;
- `EFACCONSULTARENVIOSREPORTE` discovery for submissions lacking a durable receiver id;
- Reporte Diario `DR` / `ER` / `FR` reconciliation lifecycle;
- automatic retry from ambiguous Reporte Diario `Unknown` state;
- independent cryptographic validation of DGI ACK signatures;
- Sobre v05 packaging/submission;
- authoritative external DGI Testing evidence;
- Production DGI/provider transport enablement;
- OCSP/CRL and DGI-specific certificate habilitation verification;
- production HSM/Key Vault custody;
- remaining public fiscal/status/cancellation APIs;
- general receivable collection/payment allocation workflow;
- accounts payable/procurement/treasury/cash-management completion.

## Blueprint evaluator checkpoint

Accepted historical eFactura evidence remains governed by its recorded evaluator versions and must not be rewritten retroactively.

Blueprint Master was last reverified for this lineage at `737556e24195aa909117790f2d7ff0be2fe0a474`, with root `VERSION = 0.5.2` and annotated tag `v0.5.2` resolving to that same commit.

There is no automatic consumer upgrade. Current consumer classification remains **DEFER formal 0.5.2 adoption** until its separate runtime/label migration items are deliberately executed and approved.

## Next bounded implementation sequence

PR #79 must close before later Reporte Diario response/reconciliation work advances.

After PR #79 is accepted, the next exact slice must again be revalidated against current authoritative DGI material. Candidate boundaries include:

1. authoritative receiver-id discovery through `EFACCONSULTARENVIOSREPORTE` for cases where local delivery is `Unknown` and no receiver id was durably obtained;
2. durable later-state handling for DGI `DR`, `ER` and `FR` without inventing supersession semantics;
3. separately evidenced `R05` sequence recovery;
4. Sobre v05 packaging/submission;
5. external DGI Testing evidence using legitimate credentials/certificate material outside source control;
6. Production transport only after explicit technical and operational review.

## Known non-blocking modernization debt

Green builds may still report advisory legacy debt including deprecated/outdated dependencies, Application Insights legacy APIs, old ASP.NET abstractions, provider/design packages, xUnit deprecation notices, nullable/analyzer warnings, obsolete cryptography APIs and Windows-only `System.Drawing` usage.

These items remain inventory for later bounded modernization slices and must not be upgraded wholesale without compatibility analysis.

## Repository governance at this checkpoint

- accepted `main`: `0846d63b02db2cff65bc3c86c4eca4cffa20b409`;
- accepted merge: PR #78 `feat(fiscal): add BR same-SecEnvio correction lifecycle`;
- approved PR #78 head: `22039a9b3e43807f154ea851afb9eb3863ca261e`;
- exact-head PR #78 Clean Architecture Guard #300 (`34722170807`): SUCCESS;
- post-merge Clean Architecture Guard #301 (`34722941078`): SUCCESS;
- open governed increment: PR #79 `feat(fiscal): add daily report response consultation foundation`;
- PR #79 remains pending until exact-head CI is green and human review is complete;
- one atomic slice per PR remains required;
- Blueprint 0.5.2 consumer adoption remains DEFER;
- merge requires final exact-head green CI and explicit human approval.
