# Blueprint Current State

Status: CURRENT HUMAN CHECKPOINT

Checkpoint date: 2026-09-15

Accepted functional baseline: `main@319bf1ffd6a24b31de344169fbe43d98c973a9fe`
(merge of PR #108, `feat(fiscal): compose explicit ACKCFE evidence cycle`).

There is no pending governed increment currently open. This checkpoint reconciliation is governance-only and adds no product capability. PR #108 is the latest product-capability increment; PR #104 is the latest prior full governance checkpoint reconciliation, while PR #105 and PR #107 are parallel UI governance/documentation increments only.

This file is the current human-readable checkpoint for the eFactura brownfield modernization. It does not replace requirements, architecture, API contracts or numbered implementation records. Files under `documentation/blueprint-brownfield/` remain historical inspection/remediation evidence and must not be rewritten to make the original AS-IS observations look current.

## Current validated baseline

- Runtime target: `.NET 10`.
- SDK pinned by `global.json`: `10.0.400`.
- Dedicated CI runner: `efactura-ci-01` on machine `Elena`.
- Current workflow selector: `[self-hosted, linux, x64, efactura-ci]`.
- CI database services: PostgreSQL 16 and MySQL 8.4 as isolated disposable service containers.
- NuGet vulnerability gate blocks known direct/transitive vulnerable packages.
- Clean Architecture + Ports & Adapters remains mandatory.

The accepted `main` commit is the GitHub-verified merge of PR #108. Its post-merge Clean Architecture Guard #480, run `34964273782`, completed successfully on exact `main@319bf1ffd6a24b31de344169fbe43d98c973a9fe`, push event, attempt 1, with no rerun. NuGet vulnerability gating passed; Build completed successfully; ArchitectureTests completed **207/207 passed**, CrossCuttingTests **351/351 passed**, legacy UnitTest **21/21 passed**, and the PostgreSQL/MySQL provider-real suite completed **239/239 passed**. The first job therefore completed **579/579 passed**. The provider-real run used PostgreSQL 16.15 and MySQL 8.4.11; container setup, teardown, network cleanup and orphan-process cleanup passed.

PR #106 adds a bounded read-only known-document coverage boundary over all durable ACKCFE observations for one exact accepted ACKSobre lineage. It aggregates only exact XMLDSig-verified and PKI-Uruguay-trusted evidence, deduplicates exact response bytes for assessment, fails closed on contradictory trusted detail states or response-id/hash conflicts, and classifies current knowledge as `NoDocumentCoverage`, `PartialDocumentCoverage` or `FullDocumentCoverage`. `FullDocumentCoverage` is current trusted known coverage only, not proof that DGI emitted a last message.

PR #108 composes the accepted token consultation, ACKCFE XMLDSig verification, PKI Uruguay trust and known-document coverage boundaries into one explicit caller-triggered evidence cycle. The same caller-supplied `OperationId` is reused across its durable consultation/trust checkpoints so the same operation can replay/resume existing evidence; a new operation id represents another explicit caller-requested consultation. The cycle adds no polling loop, scheduler, retry cadence, maximum retry, business timeout, token-exhaustion inference, protocol-finality inference, local fiscal/business mutation, DGI signer identity/habilitation claim or public REST endpoint.

The composed result remains explicit: `DgiIdentityValidated = false`, `ProtocolFinalityProven = false`, `TokenExhaustionProven = false` and `AutomaticReconsultationAuthorized = false`. Formal DGI Testing readiness remains unchanged.

## Historical checkpoint continuity

The immediately preceding accepted full governance checkpoint was PR #104, `docs(governance): reconcile checkpoint after PR #103`, over the accepted PR #103 product line. PR #105 (`docs(ui): establish governed visual workflow`) and PR #107 (`docs(ui): specify governed POS view`) are parallel UI governance/documentation increments and do not add backend runtime behavior, API behavior or fiscal semantics. PR #107 records the governed WEB-003 -> UI-POS-001 POS view specification and preserves its remaining price/payment discovery gaps rather than inventing contracts.

The earlier accepted checkpoint `main@6c51b0ab256af45cef15f2948ef27f2eacd1b98a`, the merge of PR #103, remains the accepted source for ACKCFE authoritative detail-state semantics. Its post-merge Clean Architecture Guard #469, run `34900941026`, completed successfully on exact `main@6c51b0ab256af45cef15f2948ef27f2eacd1b98a`.

The immediately preceding accepted checkpoint before PR #103 was `main@7c9587ecb4f08c19a78b73c8e43e24a84abe23e5`, the merge of PR #102, `docs(governance): reconcile checkpoint after PR #101`. PR #102 was governance-only and added no product/runtime behavior or fiscal semantics. Its post-merge Clean Architecture Guard #466, run `34873412495`, completed successfully on exact `main@7c9587ecb4f08c19a78b73c8e43e24a84abe23e5`, attempt 1.

PR #101 remains the accepted source for the separate ACKCFE PKI Uruguay trust boundary. PR #100 was the prior governance-only reconciliation after the PR #99 recovery. PR #98 merged but was not accepted as a governed baseline because its push-triggered Guard failed one provider-real MySQL scheduler-sensitive concurrency test; PR #99 repaired only that test harness and recovered the governed line before later increments advanced product capability.

PR #95 is part of the accepted baseline history. The earlier accepted checkpoint `main@8a70631cc5e2d723f88209459e5b77e487b66c20`, the merge of PR #95, remains the accepted source for token-input ACKCFE document-response consultation. At that checkpoint the repository recorded: `There is no pending governed increment currently open`. That historical statement remains valid as lineage, not as the current product boundary.

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
42. Append-only ACKSobre XMLDSig cryptographic verification with exact source-response SHA-256 lineage, bounded whole-document signature policy, embedded X.509 evidence, provider-real replay/concurrency convergence and explicit `CertificateTrustValidated = false` trust boundary on the signature-math record.
43. Read-only authoritative DGI CFE-state consultation through `ws_consultas / EFACCONSULTARESTADOCFE` using the already durable `TipoCFE + Serie + Nro` identity, append-only exact response XML/SHA-256 evidence, provider-real replay protection and no mutation or invented `EstadoCFE` semantics.
44. Append-only PKI Uruguay certificate-trust validation for an already verified ACKSobre signature, with exact embedded-certificate SHA-256 continuity, externally configured and SHA-256-pinned CA material, `X509Chain CustomRootTrust`, online revocation for EntireChain, provider-real replay/concurrency convergence, `PkiUruguayTrustValidated = true` and explicit `DgiIdentityValidated = false`.
45. Deterministic local Sobre batch planning for explicitly caller-selected, already-signed CFE: exact certificate thumbprint + serial-number grouping, first-seen certificate-group ordering, caller order preserved inside each certificate group, max 250 CFE per batch and local-only `BatchOrdinal`, without pending-document discovery, `Idemisor` allocation, XML packaging, persistence, transport or DGI-state interpretation.
46. Read-only ACKCFE document-response consultation by ACKSobre token through `WS_eFactura.EFACCONSULTARESTADOENVIO`, using an accepted durable `IdReceptor + Token`, exact response XML/SHA-256 evidence, strict Sobre/CFE correlation, partial-response support and append-only PostgreSQL/MySQL persistence without local lifecycle mutation or invented `ACKCFE_det/Estado` semantics.
47. Append-only whole-document ACKCFE XMLDSig signature-math verification over the exact durable consultation response, with bounded algorithm/reference policy, embedded X.509 evidence, source SHA-256 continuity, provider-real replay/concurrency convergence and explicit `CertificateTrustValidated = false`, without PKI trust, DGI signer identity or state semantics.
48. Append-only ACKCFE PKI Uruguay certificate-trust validation after successful ACKCFE signature-math verification, reusing the governed pinned-root `CustomRootTrust`/online-`EntireChain` revocation policy while persisting independent trust evidence, with exact certificate/response lineage, provider-real replay/concurrency convergence, `PkiUruguayTrustValidated = true` and explicit `DgiIdentityValidated = false`.
49. Read-only authoritative ACKCFE document-state interpretation after exact consultation/signature/trust continuity, mapping only `AE -> Received`, `BE -> Rejected`, `CE -> ObservedContingency`, enforcing message-level counter consistency and preserving fail-closed boundaries for unsupported codes, multi-message completeness/finality and all local lifecycle mutation.
50. Read-only trusted ACKCFE known-document coverage across all durable response observations for one exact accepted ACKSobre source, using only exact XMLDSig-verified and PKI-trusted evidence, deduplicating exact bytes, failing closed on trusted contradictions, and classifying `NoDocumentCoverage`, `PartialDocumentCoverage` or `FullDocumentCoverage` without treating full known coverage as protocol finality.
51. Explicit caller-triggered ACKCFE evidence-cycle composition `consultation -> XMLDSig -> PKI Uruguay trust -> known-document coverage`, with one caller-supplied `OperationId` providing replay/resume checkpoint semantics, a new operation id representing another explicit caller-requested consultation, and no automatic reconsultation, finality/exhaustion inference or local lifecycle mutation.

Detailed bounded evidence remains under `documentation/blueprint-api-implementation/` through accepted implementation record `69_FISCAL_CFE_DOCUMENT_RESPONSE_EXPLICIT_EVIDENCE_CYCLE.md`.

## Accepted Reporte Diario boundary after PR #82

The accepted Reporte Diario path remains evidence-driven and append-oriented: signed report -> durable submission -> `EFACRECEPCIONREPORTE` -> immediate AR/BR -> optional same-`SecEnvio` BR correction -> receiver discovery/response consultation when needed -> append-only DR/ER/FR observation -> read-only reconciliation assessment.

For an ordinary BR, corrected resubmission preserves the DGI `SecEnvio` while creating new immutable local correction evidence. `R05` cannot authorize this path and remains fail-closed.

A known durable `IdReceptor` can be used to obtain the original `ACKRepDiario` through the accepted consultation boundary. For an ambiguous `Unknown` delivery with no receiver id, the accepted receiver-discovery boundary queries DGI by authoritative `FechaResumen + Secuencia`, subtracts receiver ids already accounted for under the same local fiscal identity, and persists only when exactly one unaccounted receiver remains. It never selects by timestamp or returned collection order.

For an exact known receiver, the accepted later-state boundary can persist `DR` (Processed), `ER` (InManagement) and `FR` (Reliquidated) observations with raw DGI evidence. The accepted reconciliation policy interprets them locally without authorizing automatic reliquidation or local mutation. `ER` remains a manual-review boundary.

The accepted reconciliation safety flags remain explicit: `AutomaticReliquidationAuthorized = false` and `AutomaticLocalMutationAuthorized = false`. `Unknown` is never automatically retried because DGI may already have received the bytes.

## Accepted Sobre boundary after PR #108

The accepted CFE Sobre path now reaches durable signed CFE artifacts -> deterministic local Sobre batch planning -> packaging -> durable Sobre identity -> `EFACRECEPCIONSOBRE` transport -> structural ACKSobre observation -> ACKSobre signature-math verification -> optional accepted ACKSobre PKI Uruguay chain trust -> ACKCFE consultation -> ACKCFE signature-math verification -> ACKCFE PKI Uruguay chain trust -> authoritative ACKCFE per-document state interpretation -> trusted known-document coverage -> explicit caller-triggered evidence-cycle composition.

`Idemisor` remains explicit caller input. The planner is **caller-selected CFE only**: it uses first-seen certificate-group ordering, caller order preserved inside each certificate group and max 250 CFE per batch. It does not discover pending CFE and does not allocate `Idemisor`.

`ResponseReceived` remains transport evidence only. The accepted ACK observation maps AS/BS structurally, preserves S01..S08, and **S08 authorizes no automatic recovery**.

From an accepted structural `AS / Received` ACKSobre carrying non-empty `IdReceptor + Token`, the accepted document-response branch is:

```text
accepted durable ACKSobre
-> IdReceptor + Token source evidence
-> explicit caller-triggered WS_eFactura.EFACCONSULTARESTADOENVIO
-> ConsultaCFE direct CDATA
-> ACKCFE exact XML + SHA-256
-> strict Sobre/CFE correlation
-> partial ACKCFE response allowed
-> append-only consultation evidence
-> whole-document ACKCFE XMLDSig signature-math verification
-> embedded X.509 evidence
-> signature-math record keeps CertificateTrustValidated = false
-> separate PKI Uruguay chain/revocation validation
-> PkiUruguayTrustValidated = true
-> DgiIdentityValidated = false
-> trusted semantic mapping AE / BE / CE only
-> aggregate currently known trusted durable ACKCFE observations
-> NoDocumentCoverage | PartialDocumentCoverage | FullDocumentCoverage
-> FullDocumentCoverage is not protocol finality
-> same OperationId may replay/resume durable evidence checkpoints
-> new OperationId represents another explicit caller-requested consultation
-> ProtocolFinalityProven = false
-> TokenExhaustionProven = false
-> AutomaticReconsultationAuthorized = false
-> no local lifecycle mutation
```

The accepted semantic states and coverage classes are evidence-only classifications. DGI permits the per-document result to arrive in one or multiple response messages. PR #106 can therefore assess what trusted documents are currently known across durable messages, but even `FullDocumentCoverage` cannot be treated as proof that DGI emitted its last ACKCFE message. PR #108 permits another consultation only when explicitly requested by a caller through a new operation id; it does not authorize automatic polling, infer token exhaustion or infer protocol finality.

## Accepted PR #87 ACKSobre signature-verification boundary

PR #87 adds bounded **append-only ACKSobre XMLDSig cryptographic verification** over the already durable ACK observation. It verifies signature mathematics and whole-document coverage only, requires an embedded X.509 certificate, keeps `CertificateTrustValidated = false`, and performs no new DGI network call. Certificate trust is a separate boundary.

The detailed accepted evidence remains in `documentation/blueprint-api-implementation/60_FISCAL_SOBRE_ACK_SIGNATURE_VERIFICATION.md`.

## Accepted PR #89 CFE-state consultation boundary

PR #89 adds a bounded **read-only CFE-state consultation** through the authoritative DGI `ws_consultas / EFACCONSULTARESTADOCFE` contract for an already durable local fiscal identity. It preserves raw `EstadoCFE`, `IdEmisor`, `IdReceptor` and optional `ParamConsulta(Token, Fechahora)` evidence and does not invent semantic meanings or local transitions.

The detailed accepted evidence remains in `documentation/blueprint-api-implementation/61_FISCAL_CFE_STATE_CONSULTATION.md`.

## Accepted PR #91 ACKSobre PKI Uruguay certificate-trust boundary

PR #91 adds bounded **append-only PKI Uruguay certificate-trust validation** after accepted ACKSobre signature-math verification. The adapter uses `X509Chain CustomRootTrust` with externally configured SHA-256-pinned trust material, online revocation for EntireChain, and exact embedded-certificate continuity. Successful evidence records `PkiUruguayTrustValidated = true` while `DgiIdentityValidated = false` remains mandatory.

The detailed accepted evidence remains in `documentation/blueprint-api-implementation/62_FISCAL_SOBRE_ACK_CERTIFICATE_TRUST.md`.

## Accepted PR #93 deterministic Sobre batch-planning boundary

PR #93 adds a bounded local **product policy** before packaging. The accepted capability is deliberately constrained to **caller-selected CFE only**, uses first-seen certificate-group ordering, keeps caller order preserved inside each certificate group and enforces max 250 CFE per batch. It does not discover pending CFE, does not allocate `Idemisor`, does not persist a batch plan, does not call DGI and does not interpret response semantics.

The detailed accepted evidence remains in `documentation/blueprint-api-implementation/63_FISCAL_SOBRE_BATCH_PLANNING.md`.

## Accepted PR #95 ACKCFE document-response consultation boundary

PR #95 adds a bounded **read-only ACKCFE document-response consultation by ACKSobre token** through `WS_eFactura.EFACCONSULTARESTADOENVIO`. The accepted capability requires durable Sobre/submission/AS evidence and a non-empty durable `IdReceptor + Token`, keeps endpoint/SOAPAction external, preserves exact ACKCFE XML plus SHA-256, strictly correlates Sobre and every returned CFE, permits partial responses, stores a hash of the source token, and preserves source evidence unchanged.

The detailed accepted evidence remains in `documentation/blueprint-api-implementation/64_FISCAL_CFE_DOCUMENT_RESPONSE_CONSULTATION.md`.

PR #95 does not itself validate the returned ACKCFE XMLDSig or PKI chain; PR #97 closed the XMLDSig signature-math portion of that historical gap and PR #101 later closed the separately governed PKI Uruguay chain-trust portion.

## Accepted PR #97 ACKCFE signature-verification boundary

PR #97 adds bounded **append-only ACKCFE XMLDSig cryptographic verification** over the exact response already stored by PR #95.

The accepted capability performs no new DGI network call, revalidates the exact consultation response SHA-256, enforces a bounded whole-document XMLDSig policy with embedded X.509 evidence and keeps `CertificateTrustValidated = false` explicitly on the signature-math evidence. It preserves consultation, ACKSobre observation, submission, Sobre, fiscal document, sale, accounting and inventory evidence unchanged.

The detailed accepted evidence is recorded in `documentation/blueprint-api-implementation/65_FISCAL_CFE_DOCUMENT_RESPONSE_SIGNATURE_VERIFICATION.md`.

A successful verification proves bounded whole-document signature mathematics only. It does not by itself build an `X509Chain`, establish PKI Uruguay trust, validate revocation, prove DGI-specific signer identity/habilitation, infer response completeness, poll automatically, retry `Unknown`, recover S08, allocate `Idemisor`, expose a public REST endpoint or establish DGI Testing/Production readiness.

## Accepted PR #101 ACKCFE PKI Uruguay certificate-trust boundary

PR #101 adds bounded **append-only ACKCFE PKI Uruguay certificate-trust validation** after accepted ACKCFE signature-math verification. It requires the exact durable consultation response/hash and a successful persisted signature-verification record before trust validation.

The adapter deliberately reuses the already-governed PKI mechanics from the ACKSobre trust boundary: externally configured SHA-256-pinned trust roots, `X509ChainTrustMode.CustomRootTrust`, configured intermediates, online revocation across `EntireChain`, no verification-flag relaxation, bounded URL retrieval and exact embedded-certificate SHA-256 continuity. ACKCFE trust evidence is persisted independently and source records remain immutable.

Successful evidence records `PkiUruguayTrustValidated = true` and mandatory `DgiIdentityValidated = false`. The capability therefore proves configured PKI Uruguay chain trust under the governed revocation policy at validation time, but it does not prove that DGI legally controls or authorizes the end-entity signer for the specific fiscal message.

The detailed accepted evidence remains in `documentation/blueprint-api-implementation/66_FISCAL_CFE_DOCUMENT_RESPONSE_CERTIFICATE_TRUST.md`.

## Accepted PR #103 ACKCFE authoritative state-semantics boundary

PR #103 adds bounded **read-only authoritative `ACKCFE_det/Estado` interpretation** after the exact durable ACKCFE consultation has successful XMLDSig verification and PKI Uruguay trust evidence.

The accepted use case requires exact organization, consultation, ACKSobre observation, submission, Sobre, response-hash and certificate-hash continuity across the durable evidence chain. It maps only the published taxonomy:

- `AE` -> `Received`;
- `BE` -> `Rejected`;
- `CE` -> `ObservedContingency`.

The semantic boundary verifies message-local counters against the interpreted details. `CantOtrosRechazados` remains source evidence only. Any unsupported state code or counter mismatch fails closed.

This boundary performs no persistence write, no transaction side effect and no local fiscal-document lifecycle mutation. It does not infer complete/final Sobre resolution, schedule token polling/reconsultation, change the accepted `Unknown` transport policy, recover S08 or claim DGI legal signer identity/habilitation. `DgiIdentityValidated = false` remains mandatory.

The detailed accepted evidence remains in `documentation/blueprint-api-implementation/67_FISCAL_CFE_DOCUMENT_RESPONSE_STATE_SEMANTICS.md`.

## Accepted PR #106 ACKCFE known-document coverage boundary

PR #106 adds bounded **read-only trusted known-document coverage** for one exact durable Sobre/ACKSobre lineage across all currently persisted ACKCFE consultation messages.

The assessment consumes only ACKCFE observations whose exact response bytes have successful XMLDSig verification and PKI Uruguay trust evidence. Exact duplicate response bytes are deduplicated for assessment without deleting append-only observations. The boundary fails closed when the same DGI response identity appears with different bytes, when trusted detail evidence contradicts itself for one CFE identity, or when exact source lineage no longer matches.

The only accepted coverage classes are:

- `NoDocumentCoverage`;
- `PartialDocumentCoverage`;
- `FullDocumentCoverage`.

`FullDocumentCoverage` means only that every CFE identity in the durable Sobre is represented by non-contradictory currently known XMLDSig-verified and PKI-trusted ACKCFE evidence. It does **not** prove that DGI emitted its last response message, does not prove token exhaustion and does not authorize automatic reconsultation or any local lifecycle mutation.

The result remains explicit: `DgiIdentityValidated = false`, `ProtocolFinalityProven = false`, `TokenExhaustionProven = false` and `AutomaticReconsultationAuthorized = false`.

The detailed accepted evidence remains in `documentation/blueprint-api-implementation/68_FISCAL_CFE_DOCUMENT_RESPONSE_KNOWN_COVERAGE.md`.

## Accepted PR #108 explicit ACKCFE evidence-cycle boundary

PR #108 adds bounded **explicit caller-triggered evidence-cycle composition** over the already accepted ACKCFE consultation, XMLDSig, PKI Uruguay trust and known-document coverage boundaries.

For one exact durable Sobre and one explicit caller-supplied `OperationId`, the cycle executes in order:

1. `ConsultFiscalCfeEnvelopeDocumentResponseUseCase`;
2. `VerifyFiscalCfeEnvelopeDocumentResponseSignatureUseCase`;
3. `ValidateFiscalCfeEnvelopeDocumentResponseCertificateTrustUseCase`;
4. `AssessFiscalCfeEnvelopeDocumentResponseCoverageUseCase`.

The same normalized `OperationId` is deliberately reused across the durable consultation and trust checkpoints. Re-executing the same operation can resume/replay already durable checkpoints. Supplying a new operation id represents another explicit caller-requested consultation of the accepted ACKSobre token.

The cycle does not pretend that the remote DGI call and later evidence writes form one global atomic transaction. Each accepted child boundary keeps its own durable transaction/checkpoint semantics. Concurrent callers racing the same previously unseen operation id are not claimed to produce exactly-once remote invocation before one durable consultation exists.

The cycle contains no loop, polling worker, scheduler, retry cadence, maximum retry count, business timeout or local timestamp gating. It does not infer token exhaustion or protocol finality, does not mutate `FiscalDocument`, sale, accounting or inventory state, does not establish DGI signer identity/habilitation and deliberately exposes no public REST endpoint yet.

The detailed accepted evidence remains in `documentation/blueprint-api-implementation/69_FISCAL_CFE_DOCUMENT_RESPONSE_EXPLICIT_EVIDENCE_CYCLE.md`.

## DGI technical baseline currently used by the consumer

The governed fiscal lineage continues to use official DGI artifacts already pinned/reviewed in numbered implementation records, including `Formato CFE v25.2`, `Formato_Sobre_v05`, `Formato Reporte CFE v13.2`, `Formato Mensajes Respuesta v19`, `XSDs_FE_V1.44.2`, the current Web Services Externos Consultas contract and the current portal copy of Servicios Web Externos Factura Electrónica.

Current authoritative response evidence supports ACKSobre and ACKCFE public-certificate/advanced-signature requirements and whole-message signature coverage. The accepted signature-verification boundaries use that evidence only for mathematical XMLDSig verification and do not infer a DGI-specific end-entity authorization policy.

DGI material tying recognized eFactura certificates to certification providers accredited before UCE and UCE evidence for PKI Uruguay are sufficient for the accepted ACKSobre and ACKCFE PKI chain-trust boundaries. They still do not prove a DGI-specific legal signer identity/habilitation rule for the end-entity certificate.

For the exact ACKCFE per-document response context, the accepted DGI response-format evidence governs `AE`, `BE` and `CE` semantics through PR #103. PR #106 now assesses current trusted known-document coverage across durable response messages, and PR #108 permits an additional consultation only as an explicit caller-triggered operation. Neither capability establishes a last-message marker, token exhaustion or protocol finality.

The accepted `EFACCONSULTARESTADOCFE` boundary remains a read-only query by durable `TipoCFE + Serie + Nro`. The returned `EstadoCFE` taxonomy and any safe local transition policy remain separately unresolved and must not be inferred from the ACKCFE detail taxonomy.

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
- DGI-specific legal signer identity/certificate habilitation policy beyond accepted ACKSobre and ACKCFE PKI Uruguay chain/revocation validation;
- automatic `Idemisor` allocation/reservation;
- automatic pending-CFE discovery or business/fiscal eligibility selection for Sobre batch planning;
- persisted batch-plan lifecycle or automatic conversion of the local batching policy into a pending-work selector;
- authoritative semantic interpretation of the returned `EstadoCFE` taxonomy and any resulting local lifecycle transition;
- DGI-specific signer identity/habilitation for the returned `ACKCFE` certificate;
- automatic token polling/reconsultation;
- token-exhaustion or authoritative protocol-finality assessment across one or multiple `ACKCFE` response messages;
- governed public REST exposure of the accepted explicit ACKCFE evidence cycle;
- local lifecycle transition from validated ACKCFE per-document semantics or known-document coverage;
- `S08` interpretation/recovery semantics beyond preserving the reason;
- automatic reconciliation of ambiguous Sobre `Unknown` delivery;
- authoritative external DGI Testing evidence;
- Production DGI/provider transport enablement;
- production HSM/Key Vault custody;
- remaining public fiscal/status/cancellation APIs;
- general receivable collection/payment allocation workflow;
- accounts payable/procurement/treasury/cash-management completion.

Cryptographic XMLDSig verification of returned `ACKCFE` is **no longer incomplete**; it was accepted in PR #97. PKI Uruguay trust validation for the returned `ACKCFE` signature is also **no longer incomplete**; it was accepted in PR #101. Authoritative `ACKCFE_det/Estado` detail-state semantics are **no longer incomplete**; they were accepted in PR #103. Trusted multi-message known-document coverage is **no longer incomplete**; it was accepted in PR #106. Explicit caller-triggered token reconsultation/evidence-cycle composition is **no longer incomplete**; it was accepted in PR #108. DGI-specific signer identity/habilitation, automatic polling/reconsultation, token exhaustion, protocol finality, governed REST exposure, `EstadoCFE` semantics and local lifecycle mutation remain separate gaps.

## Blueprint evaluator checkpoint

Accepted historical eFactura evidence remains governed by its recorded evaluator versions and must not be rewritten retroactively.

Blueprint Master was last reverified for this lineage at `737556e24195aa909117790f2d7ff0be2fe0a474`, with root `VERSION = 0.5.2` and annotated tag `v0.5.2` resolving to that same commit.

There is no automatic consumer upgrade. Current consumer classification remains **DEFER formal 0.5.2 adoption** until its separate runtime/label migration items are deliberately executed and approved.

## Next bounded implementation sequence

PR #108 is closed and accepted. Known-document coverage and explicit caller-triggered reconsultation/evidence-cycle composition are now governed capabilities; automatic polling, token exhaustion and protocol finality remain deliberately unproven.

Candidate boundaries after PR #108 are:

1. governed REST exposure of the accepted explicit ACKCFE evidence cycle, only with separately governed permission, required idempotency semantics, safe DTO projection that never exposes the raw consultation token, deliberate raw-XML policy, and an explicit distinction between first consultation and later manual reconsultation;
2. semantic interpretation of `EstadoCFE` only if an authoritative state taxonomy, exact context linkage and safe local transition policy are proven;
3. DGI-specific legal signer identity/certificate habilitation only if authoritative end-entity policy evidence is sufficient;
4. reconciliation/discovery for ambiguous Sobre `Unknown` only if authoritative service evidence exists;
5. S08 recovery only if authoritative evidence proves a safe action rather than merely the rejection reason;
6. automatic `Idemisor` allocation only if sufficient authoritative evidence is found;
7. ER inconsistency-detail retrieval only if DGI exposes sufficient authoritative evidence;
8. separately evidenced Reporte Diario `R05` sequence recovery only if the correct sequence can be proven rather than guessed;
9. external DGI Testing evidence using legitimate credentials/certificate material outside source control;
10. Production transport only after explicit technical and operational review.

The proposed REST candidate may invoke only the already governed explicit evidence-cycle use case. It must not expose a raw consultation-only route that can leave durable ACKCFE evidence without the required XMLDSig and PKI trust checkpoints, must not expose the consultation token, and must not reinterpret `FullDocumentCoverage` as protocol finality. Its API ID, authorization permission, `Idempotency-Key` contract, DTO fields and error projection require their own governed evidence before implementation.

The DGI-specific legal signer identity/certificate habilitation candidate remains unresolved: existing PKI Uruguay and certificate-policy evidence is insufficient by itself to prove an eFactura ACK signer-specific authorization/identity rule. It must remain fail-closed rather than inferred. Therefore this checkpoint does not authorize implementation of that candidate until stronger authoritative evidence is found.

The protocol-finality boundary is also conservative. Current DGI response evidence establishes that per-document results may arrive in one or multiple response messages and that ACKSobre provides token/consultation timing evidence. PR #106 can assess known trusted document coverage and PR #108 can perform another explicit caller-requested consultation, but no definitive token-exhaustion or last-message signal has been proven. Automatic reconsultation therefore remains unauthorized.

## Known non-blocking modernization debt

Green builds may still report advisory legacy debt including deprecated/outdated dependencies, Application Insights legacy APIs, old ASP.NET abstractions, provider/design packages, xUnit deprecation notices, nullable/analyzer warnings, obsolete cryptography APIs and Windows-only `System.Drawing` usage.

These items remain inventory for later bounded modernization slices and must not be upgraded wholesale without compatibility analysis.

## Repository governance at this checkpoint

- accepted `main`: `319bf1ffd6a24b31de344169fbe43d98c973a9fe`;
- latest product-capability merge: PR #108 `feat(fiscal): compose explicit ACKCFE evidence cycle`;
- previous accepted product-capability merge: PR #106 `feat(fiscal): assess ACKCFE known document coverage`;
- latest prior full governance checkpoint reconciliation: PR #104 `docs(governance): reconcile checkpoint after PR #103`;
- parallel UI governance/documentation increments: PR #105 `docs(ui): establish governed visual workflow` and PR #107 `docs(ui): specify governed POS view`; neither adds backend runtime/API behavior or fiscal semantics;
- approved PR #108 head: `81ba1eb173128f29b1790dec9edf937378262095`;
- exact-head PR #108 Clean Architecture Guard #479 (`34928198644`): SUCCESS, attempt 1, including Build, ArchitectureTests, CrossCuttingTests, legacy UnitTest and PostgreSQL/MySQL provider-real tests;
- PR #108 merge commit: `319bf1ffd6a24b31de344169fbe43d98c973a9fe`, GitHub signature valid;
- post-merge Clean Architecture Guard #480 (`34964273782`): SUCCESS, push on exact accepted merge SHA, attempt 1, no rerun;
- accepted validation counts: ArchitectureTests 207/207, CrossCuttingTests 351/351, legacy UnitTest 21/21, first job 579/579, provider-real PostgreSQL/MySQL 239/239;
- provider-real versions: PostgreSQL 16.15 and MySQL 8.4.11;
- provider container setup, teardown, network removal and orphan-process cleanup passed;
- PR #106 preserves known coverage as non-final evidence; PR #108 preserves `DgiIdentityValidated = false`, `ProtocolFinalityProven = false`, `TokenExhaustionProven = false` and `AutomaticReconsultationAuthorized = false`;
- no governed functional increment is currently open; this checkpoint reconciliation is governance-only;
- one atomic slice per PR remains required;
- Blueprint 0.5.2 consumer adoption remains DEFER;
- merge requires final exact-head green CI and explicit human approval.
