# Blueprint Current State

Status: CURRENT HUMAN CHECKPOINT

Checkpoint date: 2026-09-12

Accepted functional baseline: `main@6fd1ae6927bf3789015d3bb659bf094283ec7095`
(merge of PR #82, `feat(fiscal): add daily report reconciliation policy`).

Current pending governed increment: PR #83 `feat(fiscal): add Sobre v05 packaging`.
PR #83 is not part of the accepted baseline until its exact final head is green and the human explicitly approves merge.

This file is the current human-readable checkpoint for the eFactura brownfield modernization. It does not replace requirements, architecture, API contracts or numbered implementation records. Files under `documentation/blueprint-brownfield/` remain historical inspection/remediation evidence and must not be rewritten to make the original AS-IS observations look current.

## Current validated baseline

- Runtime target: `.NET 10`.
- SDK pinned by `global.json`: `10.0.400`.
- Dedicated CI runner: `efactura-ci-01` on machine `Elena`.
- Current workflow selector: `[self-hosted, linux, x64, efactura-ci]`.
- CI database services: PostgreSQL 16 and MySQL 8.4 as isolated disposable service containers.
- NuGet vulnerability gate blocks known direct/transitive vulnerable packages.
- Clean Architecture + Ports & Adapters remains mandatory.

The accepted `main` commit is the GitHub-verified merge of PR #82. Its post-merge Clean Architecture Guard #357, run `34733390905`, completed successfully for both Build/Architecture and PostgreSQL/MySQL transaction jobs.

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
28. Authoritative original-response consultation for a known durable DGI `IdReceptor` through current `ws_consultas / EFACCONSULTARRESPUESTAREPORTE` evidence.
29. Append-only original `ACKRepDiario` consultation evidence with SHA-256 hash, operation replay and local-consistency classification.
30. Consultation support for root submissions and BR correction revisions without automatic transport-state mutation.
31. Provider-real PostgreSQL/MySQL consultation persistence and target resolution.
32. Authoritative `EFACCONSULTARENVIOSREPORTE` receiver-id discovery for an explicit `Unknown` root/revision with no durable `IdReceptor`.
33. Fail-closed receiver association using known-receiver set subtraction, with no timestamp/order guessing, plus provider-real PostgreSQL/MySQL discovery persistence.
34. Chaining of discovered receiver evidence back into the accepted original-response consultation target reader without rewriting the source transport row.
35. Append-only observation of DGI Reporte Diario later states `DR`, `ER` and `FR` for an exact durable `IdReceptor`.
36. Provider-real PostgreSQL/MySQL later-state persistence with raw `Ackconsultaenviosreporte`, SHA-256 evidence, operation replay and proof that root/revision transport evidence remains unchanged.
37. Read-only reconciliation policy mapping `DR -> Consistent`, `ER -> ManualReviewRequired`, `FR -> ReliquidatedExternally`, always with automatic reliquidation and local mutation disabled and fail-closed timestamp ambiguity handling.

Detailed bounded evidence remains under `documentation/blueprint-api-implementation/` through accepted document `55_FISCAL_DAILY_REPORT_RECONCILIATION_POLICY.md`.

## Accepted Reporte Diario boundary after PR #82

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
-> when a durable IdReceptor is known: EFACCONSULTARRESPUESTAREPORTE
-> append-only original ACKRepDiario consultation evidence
-> when an Unknown target has no IdReceptor: EFACCONSULTARENVIOSREPORTE
-> fail-closed unique receiver discovery
-> append-only discovered IdReceptor evidence
-> EFACCONSULTARRESPUESTAREPORTE using the discovered receiver
-> append-only original ACKRepDiario consultation evidence
-> EFACCONSULTARENVIOSREPORTE for exact known IdReceptor
-> append-only DR | ER | FR later-state observation
-> read-only reconciliation assessment
-> Consistent | ManualReviewRequired | ReliquidatedExternally
```

For an ordinary BR, corrected resubmission preserves the DGI `SecEnvio` while creating new immutable local correction evidence. `R05` cannot authorize this path and remains fail-closed.

A known durable `IdReceptor` can be used to obtain the original `ACKRepDiario` through the accepted consultation boundary. The returned evidence is stored append-only and classified against any immediate local ACK, but it does not automatically rewrite root or correction transport state.

For an ambiguous `Unknown` delivery with no receiver id, the accepted receiver-discovery boundary queries DGI by authoritative `FechaResumen + Secuencia`, subtracts receiver ids already accounted for under the same local fiscal identity, and persists only when exactly one unaccounted receiver remains. It never selects by timestamp or returned collection order.

For an exact known receiver, the accepted later-state boundary can persist `DR` (Processed), `ER` (InManagement) and `FR` (Reliquidated) observations with raw DGI evidence. The accepted reconciliation policy interprets them locally without authorizing automatic reliquidation or local mutation. `ER` remains a manual-review boundary because current evidence does not expose authoritative inconsistency-detail retrieval through the governed consultation contract.

`Unknown` is never automatically retried because DGI may already have received the bytes.

## Pending PR #83 boundary, not yet accepted

PR #83 introduces bounded local **Sobre v05 packaging** for already-signed CFE artifacts.

The candidate:

- revalidates durable signed CFE hash and pinned signed-CFE XSD evidence;
- accepts an explicit ordered set of 1..250 unique fiscal document ids;
- requires all included CFE to use the same certificate;
- verifies the actual embedded public `ds:X509Certificate` against durable thumbprint/serial evidence;
- requires every included CFE issuer RUC to match the Sobre `RUCEmisor`;
- builds deterministic `EnvioCFE version="1.0"` / `Caratula version="1.0"` with explicit `RutReceptor`, `RUCEmisor`, `Idemisor`, `CantCFE`, `Fecha` and `X509Certificate`;
- removes only the standalone XML declaration before embedding each signed CFE and does not reserialize the signed root fragment;
- validates the final envelope against a local byte-pinned `EnvioCFE.xsd` closure;
- returns XML, SHA-256, certificate identity and schema evidence;
- performs no persistence, network submission, ACK parsing, retry, state mutation or private-key access.

The detailed pending evidence is recorded in:
`documentation/blueprint-api-implementation/56_FISCAL_SOBRE_V05_PACKAGING.md`.

This section describes the open candidate only. It does not promote PR #83 into the accepted baseline.

## DGI technical baseline currently used by the consumer

The governed fiscal lineage uses official DGI artifacts already pinned/reviewed in the numbered implementation records, including:

- `Formato CFE v25.2`;
- `Formato_Sobre_v05`;
- `Formato Reporte CFE v13.2`;
- `Formato Mensajes Respuesta v19`;
- `XSDs_FE_V1.44.2`;
- byte-pinned `EnvioCFE.xsd` evidence for pending PR #83, recovered from the same immutable mirror procedure already used for governed schema bytes;
- `Servicios Web Externos DGI`, code `T-5.020.00.001-000005`, version `1.9`, dated `13/05/2024`, for original-response consultation, receiver discovery and later-state observation;
- DGI FAQ v22 section 9.7 for the explicit Reporte Diario AR/BR/DR/ER/FR meanings and the accepted reconciliation-policy boundary.

No XML element, namespace, mandatory-field rule, sequence rule, response state, Testing threshold, signature requirement, endpoint or SOAPAction may be inferred from memory, legacy demo code or provider examples when authoritative DGI evidence is required.

## DGI Testing readiness

The formal traditional `Prueba de Testing` remains blocked by product capabilities and external evidence. Local implementation progress must not be represented as DGI certification or Production readiness.

Current formal readiness remains exactly:

**BLOCKED BY MISSING PRODUCT CAPABILITIES**

External DGI Testing acceptance has not been established in this repository, and Production enablement remains separately gated.

## Explicitly not complete

The accepted baseline and pending PR #83 do not complete:

- operational correction-note command/API integration into the normal sale workflow;
- export CFE families and export-specific immutable evidence;
- contingency/CFC lifecycle;
- authoritative BCU quotation acquisition and automatic exchange-rate rule selection;
- automatic recovery semantics for Reporte Diario rejection `R05`;
- ER inconsistency-detail retrieval/interpretation;
- governed human/operational approval and command creation for a reliquidating `SecEnvio + 1` report;
- local supersession/accounting mutation after observed `FR`;
- automatic retry from ambiguous Reporte Diario `Unknown` state;
- independent cryptographic validation of DGI ACK signatures;
- durable Sobre persistence and `Idemisor` allocation/replay semantics;
- Sobre v05 compression/transport/submission and ACK lifecycle;
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

PR #83 must close before Sobre transport/persistence work advances.

The attempted next-slice revalidation after PR #82 found no governed WS Consultas v1.9 method that authoritatively retrieves ER inconsistency details and no authoritative R05 mechanism that returns the correct next sequence. Those capabilities therefore remain fail-closed rather than inferred.

After PR #83 is accepted, the next exact slice must again be revalidated against current authoritative DGI material. Candidate boundaries include:

1. durable Sobre identity/persistence and replay semantics, including an evidence-backed `Idemisor` allocation rule;
2. authoritative Sobre v05 transport framing/submission if current DGI service evidence is sufficient;
3. DGI Sobre response/ACK persistence and state model as a separate transport slice;
4. ER inconsistency-detail retrieval only if DGI exposes sufficient authoritative evidence;
5. separately evidenced `R05` sequence recovery only if the correct sequence can be proven rather than guessed;
6. external DGI Testing evidence using legitimate credentials/certificate material outside source control;
7. Production transport only after explicit technical and operational review.

## Known non-blocking modernization debt

Green builds may still report advisory legacy debt including deprecated/outdated dependencies, Application Insights legacy APIs, old ASP.NET abstractions, provider/design packages, xUnit deprecation notices, nullable/analyzer warnings, obsolete cryptography APIs and Windows-only `System.Drawing` usage.

These items remain inventory for later bounded modernization slices and must not be upgraded wholesale without compatibility analysis.

## Repository governance at this checkpoint

- accepted `main`: `6fd1ae6927bf3789015d3bb659bf094283ec7095`;
- accepted merge: PR #82 `feat(fiscal): add daily report reconciliation policy`;
- approved PR #82 head: `dffb00b8ddc0b969e843a15ee89271cfe4a19855`;
- exact-head PR #82 Clean Architecture Guard #356 (`34732631497`): SUCCESS;
- post-merge Clean Architecture Guard #357 (`34733390905`): SUCCESS;
- open governed increment: PR #83 `feat(fiscal): add Sobre v05 packaging`;
- PR #83 remains pending until exact-head CI is green and human review is complete;
- one atomic slice per PR remains required;
- Blueprint 0.5.2 consumer adoption remains DEFER;
- merge requires final exact-head green CI and explicit human approval.
