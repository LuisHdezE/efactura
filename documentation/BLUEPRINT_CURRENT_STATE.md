# Blueprint Current State

Status: CURRENT HUMAN CHECKPOINT

Checkpoint date: 2026-09-14

Accepted functional baseline: `main@0eb56872bd45dcabaf6c8ed66d7c0de1e97f5466`
(merge of PR #101, `feat(fiscal): validate ACKCFE PKI Uruguay trust`).

There is no pending governed increment currently open. This checkpoint reconciliation is governance-only and adds no product capability. PR #101 is the latest product-capability increment; PR #100 reconciled the previously recovered PR #99 baseline and PR #101 then added the separately governed ACKCFE PKI Uruguay certificate-trust boundary.

This file is the current human-readable checkpoint for the eFactura brownfield modernization. It does not replace requirements, architecture, API contracts or numbered implementation records. Files under `documentation/blueprint-brownfield/` remain historical inspection/remediation evidence and must not be rewritten to make the original AS-IS observations look current.

## Current validated baseline

- Runtime target: `.NET 10`.
- SDK pinned by `global.json`: `10.0.400`.
- Dedicated CI runner: `efactura-ci-01` on machine `Elena`.
- Current workflow selector: `[self-hosted, linux, x64, efactura-ci]`.
- CI database services: PostgreSQL 16 and MySQL 8.4 as isolated disposable service containers.
- NuGet vulnerability gate blocks known direct/transitive vulnerable packages.
- Clean Architecture + Ports & Adapters remains mandatory.

The accepted `main` commit is the GitHub-verified merge of PR #101. Its post-merge Clean Architecture Guard #463, run `34846564398`, completed successfully on exact `main@0eb56872bd45dcabaf6c8ed66d7c0de1e97f5466`, push event, attempt 1, with no rerun. NuGet vulnerability gating passed; Build completed successfully; ArchitectureTests completed **198/198 passed**, CrossCuttingTests **334/334 passed**, legacy UnitTest **21/21 passed**, and the PostgreSQL/MySQL provider-real suite completed **237/237 passed**. The first job therefore completed **553/553 passed**. The provider-real run used PostgreSQL 16.15 and MySQL 8.4.11; all six new ACKCFE certificate-trust provider executions passed across both providers; container setup, teardown, network cleanup and orphan-process cleanup passed.

PR #101 adds the bounded ACKCFE PKI Uruguay trust capability only. It does not prove DGI-specific signer identity/habilitation, interpret `ACKCFE_det/Estado` or `EstadoCFE`, infer response finality/completeness, mutate local lifecycle state or change formal DGI Testing readiness. Its accepted trust record keeps `PkiUruguayTrustValidated = true` and `DgiIdentityValidated = false`.

## Historical checkpoint continuity

The immediately preceding accepted checkpoint was `main@8245d90c938c49edc5a1b1c98a524fae81c9ab60`, the merge of PR #100, `docs(governance): reconcile checkpoint after PR #99 recovery`. PR #100 was governance-only and added no product/runtime behavior or fiscal semantics. Its post-merge Clean Architecture Guard #460, run `34841417850`, completed successfully on exact `main@8245d90c938c49edc5a1b1c98a524fae81c9ab60`, attempt 1.

PR #95 is part of the accepted baseline history. The earlier accepted checkpoint `main@8a70631cc5e2d723f88209459e5b77e487b66c20`, the merge of PR #95, remains the accepted source for token-input ACKCFE document-response consultation. At that checkpoint the repository recorded: `There is no pending governed increment currently open`. That historical statement remains valid as lineage, not as the current product boundary.

PR #98 (`docs(governance): reconcile checkpoint after PR #97`) merged as `main@a93123af3bf40f90721b8be2df378023d165d897`, but its push-triggered Clean Architecture Guard #456, run `34805279258`, failed one provider-real MySQL concurrency test at the test-only serialization barrier, so that merge was not accepted as a governed baseline. PR #99 repaired only that scheduler-sensitive test harness by ensuring both operations are independently started before awaiting convergence. Exact-head Guard #457, run `34810614265`, passed before merge, and post-merge Guard #458, run `34837067593`, then passed on exact `main@3789b5f07b9b791c938e65f8d6f643d755c9b559`. PR #100 then reconciled that recovered checkpoint before PR #101 advanced product capability.

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
44. Append-only PKI Uruguay certificate-trust validation for an already verified ACKSobre signature, with exact embedded-certificate SHA-256 continuity, externally configured and SHA-256-pinned CA material, .NET `CustomRootTrust`, online `EntireChain` revocation checking, provider-real replay/concurrency convergence, `PkiUruguayTrustValidated = true` and explicit `DgiIdentityValidated = false`.
45. Deterministic local Sobre batch planning for explicitly caller-selected, already-signed CFE: exact certificate thumbprint + serial-number grouping, first-seen certificate-group ordering, caller order preserved inside each certificate group, max 250 CFE per batch and local-only `BatchOrdinal`, without pending-document discovery, `Idemisor` allocation, XML packaging, persistence, transport or DGI-state interpretation.
46. Read-only ACKCFE document-response consultation by ACKSobre token through `WS_eFactura.EFACCONSULTARESTADOENVIO`, using an accepted durable `IdReceptor + Token`, exact response XML/SHA-256 evidence, strict Sobre/CFE correlation, partial-response support and append-only PostgreSQL/MySQL persistence without local lifecycle mutation or invented `ACKCFE_det/Estado` semantics.
47. Append-only whole-document ACKCFE XMLDSig signature-math verification over the exact durable consultation response, with bounded algorithm/reference policy, embedded X.509 evidence, source SHA-256 continuity, provider-real replay/concurrency convergence and explicit `CertificateTrustValidated = false`, without PKI trust, DGI signer identity or state semantics.
48. Append-only ACKCFE PKI Uruguay certificate-trust validation after successful ACKCFE signature-math verification, reusing the governed pinned-root `CustomRootTrust`/online-`EntireChain` revocation policy while persisting independent trust evidence, with exact certificate/response lineage, provider-real replay/concurrency convergence, `PkiUruguayTrustValidated = true` and explicit `DgiIdentityValidated = false`.

Detailed bounded evidence remains under `documentation/blueprint-api-implementation/` through accepted implementation record `66_FISCAL_CFE_DOCUMENT_RESPONSE_CERTIFICATE_TRUST.md`.

## Accepted Reporte Diario boundary after PR #82

The accepted Reporte Diario path remains evidence-driven and append-oriented: signed report -> durable submission -> `EFACRECEPCIONREPORTE` -> immediate AR/BR -> optional same-`SecEnvio` BR correction -> receiver discovery/response consultation when needed -> append-only DR/ER/FR observation -> read-only reconciliation assessment.

For an ordinary BR, corrected resubmission preserves the DGI `SecEnvio` while creating new immutable local correction evidence. `R05` cannot authorize this path and remains fail-closed.

A known durable `IdReceptor` can be used to obtain the original `ACKRepDiario` through the accepted consultation boundary. For an ambiguous `Unknown` delivery with no receiver id, the accepted receiver-discovery boundary queries DGI by authoritative `FechaResumen + Secuencia`, subtracts receiver ids already accounted for under the same local fiscal identity, and persists only when exactly one unaccounted receiver remains. It never selects by timestamp or returned collection order.

For an exact known receiver, the accepted later-state boundary can persist `DR` (Processed), `ER` (InManagement) and `FR` (Reliquidated) observations with raw DGI evidence. The accepted reconciliation policy interprets them locally without authorizing automatic reliquidation or local mutation. `ER` remains a manual-review boundary.

The accepted reconciliation safety flags remain explicit: `AutomaticReliquidationAuthorized = false` and `AutomaticLocalMutationAuthorized = false`. `Unknown` is never automatically retried because DGI may already have received the bytes.

## Accepted Sobre boundary after PR #101

The accepted CFE Sobre path now reaches durable signed CFE artifacts -> deterministic local Sobre batch planning -> packaging -> durable Sobre identity -> `EFACRECEPCIONSOBRE` transport -> structural ACKSobre observation -> ACKSobre signature-math verification -> optional accepted ACKSobre PKI Uruguay chain trust -> ACKCFE consultation -> ACKCFE signature-math verification -> ACKCFE PKI Uruguay chain trust.

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
-> no local lifecycle mutation
```

The token consultation and subsequent chain trust do not imply DGI-specific legal signer identity/habilitation, `ACKCFE_det/Estado` semantics, response completeness/finality or local lifecycle transitions.

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

The accepted capability:

- performs no new DGI network call;
- revalidates the exact consultation response SHA-256 before cryptography;
- requires root `ACKCFE` in `http://cfe.dgi.gub.uy`;
- prohibits DTD processing and external XML resolution;
- requires exactly one direct child XMLDSig `Signature` and one embedded X.509 certificate;
- requires exactly one whole-document `Reference URI=""`;
- rejects external/fragment references fail-closed;
- uses bounded canonicalization, signature, digest and transform allowlists;
- requires an RSA public key;
- verifies with `SignedXml.CheckSignature(certificate, verifySignatureOnly: true)`;
- persists append-only source lineage, algorithms and embedded-certificate evidence;
- enforces one verification per consultation with provider-real concurrency convergence;
- keeps `CertificateTrustValidated = false` explicitly on the signature-math evidence;
- preserves consultation, ACKSobre observation, submission, Sobre, fiscal document, sale, accounting and inventory evidence unchanged.

The detailed accepted evidence is recorded in `documentation/blueprint-api-implementation/65_FISCAL_CFE_DOCUMENT_RESPONSE_SIGNATURE_VERIFICATION.md`.

A successful verification proves bounded whole-document signature mathematics only. It does **not** by itself build an `X509Chain`, establish PKI Uruguay trust, validate revocation, prove DGI-specific signer identity/habilitation, interpret `ACKCFE_det/Estado`, reinterpret `EstadoCFE`, infer response completeness, poll automatically, retry `Unknown`, recover S08, allocate `Idemisor`, expose a public REST endpoint or establish DGI Testing/Production readiness. PR #101 adds the separate chain-trust boundary without rewriting this evidence.

## Accepted PR #101 ACKCFE PKI Uruguay certificate-trust boundary

PR #101 adds bounded **append-only ACKCFE PKI Uruguay certificate-trust validation** after accepted ACKCFE signature-math verification. It requires the exact durable consultation response/hash and a successful persisted signature-verification record before trust validation.

The adapter deliberately reuses the already-governed PKI mechanics from the ACKSobre trust boundary: externally configured SHA-256-pinned trust roots, `X509ChainTrustMode.CustomRootTrust`, configured intermediates, online revocation across `EntireChain`, no verification-flag relaxation, bounded URL retrieval and exact embedded-certificate SHA-256 continuity. ACKCFE trust evidence is persisted independently and source records remain immutable.

Successful evidence records `PkiUruguayTrustValidated = true` and mandatory `DgiIdentityValidated = false`. The capability therefore proves configured PKI Uruguay chain trust under the governed revocation policy at validation time, but it does not prove that DGI legally controls or authorizes the end-entity signer for the specific fiscal message.

The detailed accepted evidence remains in `documentation/blueprint-api-implementation/66_FISCAL_CFE_DOCUMENT_RESPONSE_CERTIFICATE_TRUST.md`.

## DGI technical baseline currently used by the consumer

The governed fiscal lineage continues to use official DGI artifacts already pinned/reviewed in numbered implementation records, including `Formato CFE v25.2`, `Formato_Sobre_v05`, `Formato Reporte CFE v13.2`, `Formato Mensajes Respuesta v19`, `XSDs_FE_V1.44.2`, the current Web Services Externos Consultas contract and the current portal copy of Servicios Web Externos Factura Electrónica.

Current authoritative response evidence supports ACKSobre and ACKCFE public-certificate/advanced-signature requirements and whole-message signature coverage. The accepted signature-verification boundaries use that evidence only for mathematical XMLDSig verification and do not infer a DGI-specific end-entity authorization policy.

DGI material tying recognized eFactura certificates to certification providers accredited before UCE and UCE evidence for PKI Uruguay are sufficient for the accepted ACKSobre and ACKCFE PKI chain-trust boundaries. They still do not prove a DGI-specific legal signer identity/habilitation rule for the end-entity certificate.

The accepted `EFACCONSULTARESTADOCFE` boundary remains a read-only query by durable `TipoCFE + Serie + Nro`. The current DGI response-format lineage identifies response codes such as AE/BE/CE in relevant response contexts, but no local `EstadoCFE` transition policy is accepted until the authoritative taxonomy and context linkage are sufficient to avoid invented semantics.

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
- authoritative semantic interpretation of `ACKCFE_det/Estado` and any resulting local lifecycle transition;
- automatic token polling/reconsultation or authoritative completeness assessment across multiple `ACKCFE` messages;
- `S08` interpretation/recovery semantics beyond preserving the reason;
- automatic reconciliation of ambiguous Sobre `Unknown` delivery;
- authoritative external DGI Testing evidence;
- Production DGI/provider transport enablement;
- production HSM/Key Vault custody;
- remaining public fiscal/status/cancellation APIs;
- general receivable collection/payment allocation workflow;
- accounts payable/procurement/treasury/cash-management completion.

cryptographic XMLDSig verification of returned `ACKCFE` is **no longer incomplete**; it was accepted in PR #97. PKI Uruguay trust validation for the returned `ACKCFE` signature is also **no longer incomplete**; it was accepted in PR #101. DGI-specific signer identity/habilitation, response semantics and completeness/finality remain separate gaps.

## Blueprint evaluator checkpoint

Accepted historical eFactura evidence remains governed by its recorded evaluator versions and must not be rewritten retroactively.

Blueprint Master was last reverified for this lineage at `737556e24195aa909117790f2d7ff0be2fe0a474`, with root `VERSION = 0.5.2` and annotated tag `v0.5.2` resolving to that same commit.

There is no automatic consumer upgrade. Current consumer classification remains **DEFER formal 0.5.2 adoption** until its separate runtime/label migration items are deliberately executed and approved.

## Next bounded implementation sequence

PR #101 is closed and accepted. Later identity, response interpretation, recovery and product-capability work may advance only through separately governed, evidence-backed increments.

Candidate boundaries after PR #101 are:

1. DGI-specific legal signer identity/certificate habilitation only if authoritative end-entity policy evidence is sufficient;
2. semantic interpretation of `ACKCFE_det/Estado` only if an authoritative taxonomy and safe local transition policy are proven;
3. semantic interpretation of `EstadoCFE` only if an authoritative state taxonomy and safe local transition policy are proven;
4. token reconsultation/completeness policy only if safe product rules can distinguish partial/evolving responses without inventing finality;
5. reconciliation/discovery for ambiguous Sobre `Unknown` only if authoritative service evidence exists;
6. S08 recovery only if authoritative evidence proves a safe action rather than merely the rejection reason;
7. automatic `Idemisor` allocation only if sufficient authoritative evidence is found;
8. ER inconsistency-detail retrieval only if DGI exposes sufficient authoritative evidence;
9. separately evidenced Reporte Diario `R05` sequence recovery only if the correct sequence can be proven rather than guessed;
10. external DGI Testing evidence using legitimate credentials/certificate material outside source control;
11. Production transport only after explicit technical and operational review.

The DGI-specific legal signer identity/certificate habilitation candidate remains unresolved: existing PKI Uruguay and certificate-policy evidence is insufficient by itself to prove an eFactura ACK signer-specific authorization/identity rule. It must remain fail-closed rather than inferred. Therefore this checkpoint does not authorize implementation of that candidate until stronger authoritative evidence is found; the next functional slice may skip to another bounded candidate only after its own evidence review.

## Known non-blocking modernization debt

Green builds may still report advisory legacy debt including deprecated/outdated dependencies, Application Insights legacy APIs, old ASP.NET abstractions, provider/design packages, xUnit deprecation notices, nullable/analyzer warnings, obsolete cryptography APIs and Windows-only `System.Drawing` usage.

These items remain inventory for later bounded modernization slices and must not be upgraded wholesale without compatibility analysis.

## Repository governance at this checkpoint

- accepted `main`: `0eb56872bd45dcabaf6c8ed66d7c0de1e97f5466`;
- latest product-capability merge: PR #101 `feat(fiscal): validate ACKCFE PKI Uruguay trust`;
- latest governance-only checkpoint merge before PR #101: PR #100 `docs(governance): reconcile checkpoint after PR #99 recovery` -> `8245d90c938c49edc5a1b1c98a524fae81c9ab60`;
- PR #100 post-merge Clean Architecture Guard #460 (`34841417850`): SUCCESS, push on exact accepted merge SHA, attempt 1;
- approved PR #101 head: `343b4ed8aebcd162eada9662cad9216a05160671`;
- exact-head PR #101 Clean Architecture Guard #462 (`34844303667`): SUCCESS, attempt 1, no rerun, first job 553/553 and provider-real 237/237;
- PR #101 merge commit: `0eb56872bd45dcabaf6c8ed66d7c0de1e97f5466`, GitHub signature valid;
- post-merge Clean Architecture Guard #463 (`34846564398`): SUCCESS, push on exact accepted merge SHA, attempt 1, no rerun;
- accepted validation counts: ArchitectureTests 198/198, CrossCuttingTests 334/334, legacy UnitTest 21/21, first job 553/553, provider-real PostgreSQL/MySQL 237/237;
- all six new ACKCFE trust provider executions passed across PostgreSQL/MySQL;
- provider-real versions: PostgreSQL 16.15 and MySQL 8.4.11;
- provider container setup, teardown, network removal and orphan-process cleanup passed;
- PR #101 preserves `DgiIdentityValidated = false` and does not authorize state semantics, response finality/completeness or local mutation;
- no governed functional increment is currently open; this checkpoint reconciliation is governance-only;
- one atomic slice per PR remains required;
- Blueprint 0.5.2 consumer adoption remains DEFER;
- merge requires final exact-head green CI and explicit human approval.
