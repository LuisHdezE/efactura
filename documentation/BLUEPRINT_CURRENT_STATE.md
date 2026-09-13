# Blueprint Current State

Status: CURRENT HUMAN CHECKPOINT

Checkpoint date: 2026-09-13

Accepted functional baseline: `main@68b2b79772230d858ac3c2dde573542b3caaef97`
(merge of PR #87, `feat(fiscal): verify ACKSobre XMLDSig`).

There is no pending governed increment currently open. PR #87 is part of the accepted baseline after exact-head validation, explicit human merge approval and successful post-merge validation on `main`.

This file is the current human-readable checkpoint for the eFactura brownfield modernization. It does not replace requirements, architecture, API contracts or numbered implementation records. Files under `documentation/blueprint-brownfield/` remain historical inspection/remediation evidence and must not be rewritten to make the original AS-IS observations look current.

## Current validated baseline

- Runtime target: `.NET 10`.
- SDK pinned by `global.json`: `10.0.400`.
- Dedicated CI runner: `efactura-ci-01` on machine `Elena`.
- Current workflow selector: `[self-hosted, linux, x64, efactura-ci]`.
- CI database services: PostgreSQL 16 and MySQL 8.4 as isolated disposable service containers.
- NuGet vulnerability gate blocks known direct/transitive vulnerable packages.
- Clean Architecture + Ports & Adapters remains mandatory.

The accepted `main` commit is the GitHub-verified merge of PR #87. Its post-merge Clean Architecture Guard #415, run `34758431307`, completed successfully on exact `main@68b2b79772230d858ac3c2dde573542b3caaef97` for Build/Architecture/CrossCutting/Legacy and PostgreSQL/MySQL provider-real transaction jobs.

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
38. Deterministic local Sobre v05 packaging for 1..250 already-signed CFE, same-certificate verification, signed-subtree preservation and byte-pinned `EnvioCFE.xsd` validation without persistence or transport.
39. Durable local Sobre identity and replay persistence with explicit caller-supplied `Idemisor`, exact envelope/hash/schema/certificate evidence and provider-real PostgreSQL/MySQL concurrent replay convergence.
40. Durable Sobre transport through `EFACRECEPCIONSOBRE`, with exact direct-CDATA `EnvioCFE`, WS-Security X509, `Prepared -> InFlight -> ResponseReceived|Unknown`, provider-real dispatch serialization and no automatic retry from `Unknown`.
41. Append-only immediate `ACKSobre` observation mapping `AS -> Received` and `BS -> Rejected`, preserving DGI correlation ids, optional consultation parameters and S01..S08 evidence without CFE mutation or S08 recovery.
42. Append-only ACKSobre XMLDSig cryptographic verification with exact source-response SHA-256 lineage, bounded whole-document signature policy, embedded X.509 evidence, provider-real replay/concurrency convergence and explicit `CertificateTrustValidated = false` trust boundary.

Detailed bounded evidence remains under `documentation/blueprint-api-implementation/` through accepted document `60_FISCAL_SOBRE_ACK_SIGNATURE_VERIFICATION.md`.

## Accepted Reporte Diario boundary after PR #82

The accepted Reporte Diario path remains:

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

The accepted reconciliation safety flags remain explicit: `AutomaticReliquidationAuthorized = false` and `AutomaticLocalMutationAuthorized = false`.

`Unknown` is never automatically retried because DGI may already have received the bytes.

## Accepted Sobre boundary after PR #87

The accepted CFE Sobre path now reaches:

```text
durable signed CFE artifacts
-> signed CFE hash/XSD revalidation
-> explicit ordered 1..250 CFE selection
-> same-certificate guard
-> embedded ds:X509Certificate verification
-> issuer RUC consistency guard
-> deterministic EnvioCFE / Caratula v1.0
-> signed CFE root fragments preserved without reserialization
-> byte-pinned EnvioCFE.xsd validation
-> local Sobre XML + SHA-256 + certificate/schema evidence
-> durable local Sobre identity
-> unique OrganizationId + OperationId replay guard
-> provider-real concurrent replay convergence
-> durable transport intent
-> Prepared
-> InFlight persisted before network
-> DGI EFACRECEPCIONSOBRE via SOAP 1.1 + WS-Security X509
-> exact EnvioCFE in Datain/xmlData CDATA
-> opaque Dataout/xmlData + SHA-256
-> ResponseReceived | Unknown
-> when ResponseReceived: structural ACKSobre observation
-> AS -> Received | BS -> Rejected
-> exact RUC / IdEmisor / CantCFE correlation
-> DGI IDRespuesta + IDReceptor evidence
-> optional Token + FechaHora evidence
-> S01..S08 rejection evidence
-> append-only provider-real replay/convergence
-> exact response SHA-256 and lineage revalidation
-> bounded whole-document ACKSobre XMLDSig verification
-> embedded X.509 certificate/signature algorithm evidence
-> CertificateTrustValidated = false
-> append-only provider-real verification replay/convergence
```

The accepted byte-pinned `EnvioCFE.xsd` evidence is tied to the DGI-published `XSDs_FE_V1.44.2` registry identity and the governed immutable byte-recovery procedure recorded in document 56.

`Idemisor` remains explicit caller input because the reviewed authoritative DGI material establishes issuer assignment but no authoritative next-value allocation algorithm. The local identity is a consumer replay/correlation invariant and is not represented as DGI's duplicate-detection key for `S08`.

`ResponseReceived` remains transport evidence only. The accepted ACK observation adds typed immediate Sobre evidence, but neither `AS` nor `BS` is interpreted as individual CFE acceptance/rejection and S08 authorizes no automatic recovery.

## Accepted PR #87 ACKSobre signature-verification boundary

PR #87 adds bounded **append-only ACKSobre XMLDSig cryptographic verification** over the already durable ACK observation.

The accepted capability:

- performs no new DGI network call;
- revalidates exact response SHA-256 and durable envelope/submission/ACK lineage;
- verifies XMLDSig only behind an Infrastructure port;
- prohibits DTD/external XML resolution and external signature references;
- requires exactly one whole-document `Reference URI=""` and enveloped-signature transform;
- allows only a bounded local set of canonicalization/RSA/digest algorithms, including legacy RSA-SHA1/SHA1 solely because the official DGI ACKSobre example uses that external signature tuple;
- requires exactly one embedded X.509 certificate and an RSA public key;
- calls `SignedXml.CheckSignature(certificate, verifySignatureOnly: true)`;
- persists certificate/signature algorithm evidence plus exact source response SHA-256;
- records `CertificateTrustValidated = false` explicitly;
- uses `Restrict` FKs to ACK observation, submission and Sobre;
- replays/converges provider-real without rewriting any accepted source evidence.

The detailed accepted evidence is recorded in:
`documentation/blueprint-api-implementation/60_FISCAL_SOBRE_ACK_SIGNATURE_VERIFICATION.md`.

This accepted boundary validates signature mathematics and whole-document coverage only. It does not claim X.509 trust-chain validation, DGI legal identity, OCSP/CRL status or certificate habilitation.

## DGI technical baseline currently used by the consumer

The governed fiscal lineage uses official DGI artifacts already pinned/reviewed in the numbered implementation records, including:

- `Formato CFE v25.2`;
- `Formato_Sobre_v05`;
- `Formato Reporte CFE v13.2`;
- `Formato Mensajes Respuesta v19`;
- `XSDs_FE_V1.44.2`;
- accepted byte-pinned `EnvioCFE.xsd` evidence from PR #83 / document 56;
- DGI Production communiqué documenting duplicate-Sobre response code `S08` without establishing the exact duplicate key or recovery policy;
- current DGI `Documentos de interés` registry continuing to link **Web Services Externos Recepción** for `EFACRECEPCIONSOBRE` framing;
- DGI reception evidence defining `Datain/xmlData` with `EnvioCFE` directly in CDATA and `Dataout/xmlData` for the response;
- current response-format evidence defining `ACKSobre`, `AS`, `BS`, correlation identifiers, optional `ParamConsulta`, rejection-reason structure, public certificate evidence and advanced electronic signature over the response;
- official reception example showing a whole-document enveloped XMLDSig with embedded X.509 certificate;
- current DGI applicability evidence supporting S01..S08 at the DGI Sobre reception boundary;
- `Servicios Web Externos DGI`, code `T-5.020.00.001-000005`, version `1.9`, dated `13/05/2024`, for current consultation methods;
- DGI FAQ v22 section 9.7 for the explicit Reporte Diario AR/BR/DR/ER/FR meanings and the accepted reconciliation-policy boundary.

The current Consultas v1.9 contract exposes methods that return Sobre consultation parameters, but the reviewed authoritative material does not establish a web-service operation whose input is the ACKSobre `Token` and whose output is the second/document-level CFE response. That capability remains fail-closed rather than invented.

The gzip+Base64 routine reviewed in current Web Services Externos Consultas is consultation-specific evidence. It is not used to invent compression for `EFACRECEPCIONSOBRE` when the DGI reception document explicitly shows direct CDATA framing.

No XML element, namespace, mandatory-field rule, sequence/allocation rule, response state, Testing threshold, signature requirement, endpoint or SOAPAction may be inferred from memory, legacy demo code or provider examples when authoritative DGI evidence is required.

## DGI Testing readiness

The formal traditional `Prueba de Testing` remains blocked by product capabilities and external evidence. Local implementation progress must not be represented as DGI certification or Production readiness.

Current formal readiness remains exactly:

**BLOCKED BY MISSING PRODUCT CAPABILITIES**

External DGI Testing acceptance has not been established in this repository, and Production enablement remains separately gated.

## Explicitly not complete

The accepted baseline does not complete:

- operational correction-note command/API integration into the normal sale workflow;
- export CFE families and export-specific immutable evidence;
- contingency/CFC lifecycle;
- authoritative BCU quotation acquisition and automatic exchange-rate rule selection;
- automatic recovery semantics for Reporte Diario rejection `R05`;
- ER inconsistency-detail retrieval/interpretation;
- governed human/operational approval and command creation for a reliquidating `SecEnvio + 1` report;
- local supersession/accounting mutation after observed `FR`;
- automatic retry from ambiguous Reporte Diario `Unknown` state;
- X.509 trust-chain/trust-anchor validation for DGI ACK certificates;
- automatic `Idemisor` allocation/reservation;
- business grouping/batching policy for CFE into Sobre;
- document-level CFE response consultation/interpretation after an AS token because no governed token-input operation has been proven;
- `S08` interpretation/recovery semantics beyond preserving the reason;
- automatic reconciliation of ambiguous Sobre `Unknown` delivery;
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

PR #87 is closed and accepted. Later ACK trust/reconciliation work may advance only through separately governed, evidence-backed increments.

The authoritative revalidation after PR #86 found that the ACKSobre token is documented as consultation evidence, while the current WS Consultas v1.9 contract does not prove a token-input operation for obtaining the second CFE response. That path therefore remains fail-closed.