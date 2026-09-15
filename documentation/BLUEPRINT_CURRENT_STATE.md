# Blueprint Current State

Status: CURRENT HUMAN CHECKPOINT

Checkpoint date: 2026-09-14

Accepted functional baseline: `main@6c51b0ab256af45cef15f2948ef27f2eacd1b98a`
(merge of PR #103, `feat(fiscal): interpret authoritative ACKCFE states`).

There is no pending governed increment currently open. This checkpoint reconciliation is governance-only and adds no product capability. PR #103 is the latest product-capability increment; PR #102 is the latest governance-only checkpoint before PR #103.

This file is the current human-readable checkpoint for the eFactura brownfield modernization. It does not replace requirements, architecture, API contracts or numbered implementation records. Files under `documentation/blueprint-brownfield/` remain historical inspection/remediation evidence and must not be rewritten to make the original AS-IS observations look current.

## Current validated baseline

- Runtime target: `.NET 10`.
- SDK pinned by `global.json`: `10.0.400`.
- Dedicated CI runner: `efactura-ci-01` on machine `Elena`.
- Current workflow selector: `[self-hosted, linux, x64, efactura-ci]`.
- CI database services: PostgreSQL 16 and MySQL 8.4 as isolated disposable service containers.
- NuGet vulnerability gate blocks known direct/transitive vulnerable packages.
- Clean Architecture + Ports & Adapters remains mandatory.

The accepted `main` commit is the GitHub-verified merge of PR #103. Its post-merge Clean Architecture Guard #469, run `34900941026`, completed successfully on exact `main@6c51b0ab256af45cef15f2948ef27f2eacd1b98a`, push event, attempt 1, with no rerun. NuGet vulnerability gating passed; Build completed successfully; ArchitectureTests completed **200/200 passed**, CrossCuttingTests **339/339 passed**, legacy UnitTest **21/21 passed**, and the PostgreSQL/MySQL provider-real suite completed **237/237 passed**. The first job therefore completed **560/560 passed**. The provider-real run used PostgreSQL 16.15 and MySQL 8.4.11; container setup, teardown, network cleanup and orphan-process cleanup passed.

PR #103 adds a bounded read-only authoritative ACKCFE detail-state semantic boundary after the already accepted ACKCFE consultation, XMLDSig verification and PKI Uruguay trust chain. It maps only the DGI-published `ACKCFE_det/Estado` codes `AE -> Received`, `BE -> Rejected` and `CE -> ObservedContingency`, validates `CantCFEAceptados`, `CantCFERechazados` and `CantCFEObservados` against the interpreted details, and keeps `CantOtrosRechazados` as source evidence without inventing per-CFE meaning. Unsupported codes and counter mismatches fail closed.

PR #103 does not prove DGI-specific signer identity/habilitation, does not interpret `EstadoCFE`, does not infer response completeness/finality across one or multiple response messages, does not schedule token polling/reconsultation, does not mutate any local fiscal lifecycle and does not change formal DGI Testing readiness. `PkiUruguayTrustValidated = true` remains a PKI-chain statement only and `DgiIdentityValidated = false` remains mandatory.

## Historical checkpoint continuity

The immediately preceding accepted checkpoint was `main@7c9587ecb4f08c19a78b73c8e43e24a84abe23e5`, the merge of PR #102, `docs(governance): reconcile checkpoint after PR #101`. PR #102 was governance-only and added no product/runtime behavior or fiscal semantics. Its post-merge Clean Architecture Guard #466, run `34873412495`, completed successfully on exact `main@7c9587ecb4f08c19a78b73c8e43e24a84abe23e5`, attempt 1.

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

Detailed bounded evidence remains under `documentation/blueprint-api-implementation/` through accepted implementation record `67_FISCAL_CFE_DOCUMENT_RESPONSE_STATE_SEMANTICS.md`.

## Accepted Reporte Diario boundary after PR #82

The accepted Reporte Diario path remains evidence-driven and append-oriented: signed report -> durable submission -> `EFACRECEPCIONREPORTE` -> immediate AR/BR -> optional same-`SecEnvio` BR correction -> receiver discovery/response consultation when needed -> append-only DR/ER/FR observation -> read-only reconciliation assessment.

For an ordinary BR, corrected resubmission preserves the DGI `SecEnvio` while creating new immutable local correction evidence. `R05` cannot authorize this path and remains fail-closed.

A known durable `IdReceptor` can be used to obtain the original `ACKRepDiario` through the accepted consultation boundary. For an ambiguous `Unknown` delivery with no receiver id, the accepted receiver-discovery boundary queries DGI by authoritative `FechaResumen + Secuencia`, subtracts receiver ids already accounted for under the same local fiscal identity, and persists only when exactly one unaccounted receiver remains. It never selects by timestamp or returned collection order.

For an exact known receiver, the accepted later-state boundary can persist `DR` (Processed), `ER` (InManagement) and `FR` (Reliquidated) observations with raw DGI evidence. The accepted reconciliation policy interprets them locally without authorizing automatic reliquidation or local mutation. `ER` remains a manual-review boundary.

The accepted reconciliation safety flags remain explicit: `AutomaticReliquidationAuthorized = false` and `AutomaticLocalMutationAuthorized = false`. `Unknown` is never automatically retried because DGI may already have received the bytes.

## Accepted Sobre boundary after PR #103

The accepted CFE Sobre path now reaches durable signed CFE artifacts -> deterministic local Sobre batch planning -> packaging -> durable Sobre identity -> `EFACRECEPCIONSOBRE` transport -> structural ACKSobre observation -> ACKSobre signature-math verification -> optional accepted ACKSobre PKI Uruguay chain trust -> ACKCFE consultation -> ACKCFE signature-math verification -> ACKCFE PKI Uruguay chain trust -> authoritative ACKCFE per-document state interpretation.

`Idemisor` remains explicit caller input. The planner is **caller-selected CFE only**: it uses first-seen certificate-group ordering, caller order preserved inside each certificate group and max 250 CFE per batch. It does not discover pending CFE and does not allocate `Idemisor`.

`ResponseReceived` remains transport evidence only. The accepted ACK observation maps AS/BS structurally, preserves S01..S08, and **S08 authorizes no automatic recovery**.

From an accepted structural `AS / Received` ACKSobre carrying non-empty `IdReceptor + Token`, the accepted document-response branch is:

```text
accepted durable ACKSobre
-> IdReceptor + Token source evidence
-> WS_eFactura.EFACCONSULTARESTADOENVIO
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
-> no complete Sobre-resolution claim
-> no token finality inference
-> no local lifecycle mutation
```

The accepted semantic states are evidence-only classifications. DGI permits the per-document result to arrive in one or multiple response messages, so a single ACKCFE cannot be treated as proof that the Sobre is complete or final. Token reconsultation/completeness policy remains a separate bounded candidate.

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

## DGI technical baseline currently used by the consumer

The governed fiscal lineage continues to use official DGI artifacts already pinned/reviewed in numbered implementation records, including `Formato CFE v25.2`, `Formato_Sobre_v05`, `Formato Reporte CFE v13.2`, `Formato Mensajes Respuesta v19`, `XSDs_FE_V1.44.2`, the current Web Services Externos Consultas contract and the current portal copy of Servicios Web Externos Factura Electrónica.

Current authoritative response evidence supports ACKSobre and ACKCFE public-certificate/advanced-signature requirements and whole-message signature coverage. The accepted signature-verification boundaries use that evidence only for mathematical XMLDSig verification and do not infer a DGI-specific end-entity authorization policy.

DGI material tying recognized eFactura certificates to certification providers accredited before UCE and UCE evidence for PKI Uruguay are sufficient for the accepted ACKSobre and ACKCFE PKI chain-trust boundaries. They still do not prove a DGI-specific legal signer identity/habilitation rule for the end-entity certificate.

For the exact ACKCFE per-document response context, the accepted DGI response-format evidence now governs `AE`, `BE` and `CE` semantics through PR #103. That acceptance is message-local only and does not turn one response into a completeness/finality signal.

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
- authoritative completeness/finality assessment across one or multiple `ACKCFE` response messages;
- local lifecycle transition from validated ACKCFE per-document semantics;
- `S08` interpretation/recovery semantics beyond preserving the reason;
- automatic reconciliation of ambiguous Sobre `Unknown` delivery;
- authoritative external DGI Testing evidence;
- Production DGI/provider transport enablement;
- production HSM/Key Vault custody;
- remaining public fiscal/status/cancellation APIs;
- general receivable collection/payment allocation workflow;
- accounts payable/procurement/treasury/cash-management completion.

cryptographic XMLDSig verification of returned `ACKCFE` is **no longer incomplete**; it was accepted in PR #97. PKI Uruguay trust validation for the returned `ACKCFE` signature is also **no longer incomplete**; it was accepted in PR #101. Authoritative `ACKCFE_det/Estado` detail-state semantics are **no longer incomplete**; they were accepted in PR #103. DGI-specific signer identity/habilitation, multi-message completeness/finality, token reconsultation policy, `EstadoCFE` semantics and local lifecycle mutation remain separate gaps.

## Blueprint evaluator checkpoint

Accepted historical eFactura evidence remains governed by its recorded evaluator versions and must not be rewritten retroactively.

Blueprint Master was last reverified for this lineage at `737556e24195aa909117790f2d7ff0be2fe0a474`, with root `VERSION = 0.5.2` and annotated tag `v0.5.2` resolving to that same commit.

There is no automatic consumer upgrade. Current consumer classification remains **DEFER formal 0.5.2 adoption** until its separate runtime/label migration items are deliberately executed and approved.

## Next bounded implementation sequence

PR #103 is closed and accepted. Later identity, completeness/reconsultation, state interpretation, recovery and product-capability work may advance only through separately governed, evidence-backed increments.

Candidate boundaries after PR #103 are:

1. token reconsultation/completeness policy only if safe product rules can distinguish partial/evolving responses without inventing finality;
2. semantic interpretation of `EstadoCFE` only if an authoritative state taxonomy, exact context linkage and safe local transition policy are proven;
3. DGI-specific legal signer identity/certificate habilitation only if authoritative end-entity policy evidence is sufficient;
4. reconciliation/discovery for ambiguous Sobre `Unknown` only if authoritative service evidence exists;
5. S08 recovery only if authoritative evidence proves a safe action rather than merely the rejection reason;
6. automatic `Idemisor` allocation only if sufficient authoritative evidence is found;
7. ER inconsistency-detail retrieval only if DGI exposes sufficient authoritative evidence;
8. separately evidenced Reporte Diario `R05` sequence recovery only if the correct sequence can be proven rather than guessed;
9. external DGI Testing evidence using legitimate credentials/certificate material outside source control;
10. Production transport only after explicit technical and operational review.

The DGI-specific legal signer identity/certificate habilitation candidate remains unresolved: existing PKI Uruguay and certificate-policy evidence is insufficient by itself to prove an eFactura ACK signer-specific authorization/identity rule. It must remain fail-closed rather than inferred. Therefore this checkpoint does not authorize implementation of that candidate until stronger authoritative evidence is found.

The token reconsultation/completeness candidate is also bounded conservatively. Current DGI response evidence establishes that per-document results may arrive in one or multiple response messages and that ACKSobre provides token/consultation timing evidence, but this checkpoint does not claim that a definitive token-exhaustion or final-completeness signal has been proven. Any later implementation must preserve append-only observations and fail closed when completeness cannot be demonstrated.

## Known non-blocking modernization debt

Green builds may still report advisory legacy debt including deprecated/outdated dependencies, Application Insights legacy APIs, old ASP.NET abstractions, provider/design packages, xUnit deprecation notices, nullable/analyzer warnings, obsolete cryptography APIs and Windows-only `System.Drawing` usage.

These items remain inventory for later bounded modernization slices and must not be upgraded wholesale without compatibility analysis.

## Repository governance at this checkpoint

- accepted `main`: `6c51b0ab256af45cef15f2948ef27f2eacd1b98a`;
- latest product-capability merge: PR #103 `feat(fiscal): interpret authoritative ACKCFE states`;
- latest governance-only checkpoint merge before PR #103: PR #102 `docs(governance): reconcile checkpoint after PR #101` -> `7c9587ecb4f08c19a78b73c8e43e24a84abe23e5`;
- PR #102 post-merge Clean Architecture Guard #466 (`34873412495`): SUCCESS, push on exact accepted merge SHA, attempt 1;
- approved PR #103 head: `98fa5b6013734c0cdf0dd4a353114df0bc8fd303`;
- exact-head PR #103 Clean Architecture Guard #468 (`34898380425`): SUCCESS, attempt 1, no rerun, first job 560/560 and provider-real 237/237;
- PR #103 merge commit: `6c51b0ab256af45cef15f2948ef27f2eacd1b98a`, GitHub signature valid;
- post-merge Clean Architecture Guard #469 (`34900941026`): SUCCESS, push on exact accepted merge SHA, attempt 1, no rerun;
- accepted validation counts: ArchitectureTests 200/200, CrossCuttingTests 339/339, legacy UnitTest 21/21, first job 560/560, provider-real PostgreSQL/MySQL 237/237;
- provider-real versions: PostgreSQL 16.15 and MySQL 8.4.11;
- provider container setup, teardown, network removal and orphan-process cleanup passed;
- PR #103 preserves `DgiIdentityValidated = false` and does not authorize response completeness/finality, token polling/reconsultation or local lifecycle mutation;
- no governed functional increment is currently open; this checkpoint reconciliation is governance-only;
- one atomic slice per PR remains required;
- Blueprint 0.5.2 consumer adoption remains DEFER;
- merge requires final exact-head green CI and explicit human approval.
