# Blueprint Current State

Status: CURRENT HUMAN CHECKPOINT

Checkpoint date: 2026-09-13

Accepted functional baseline: `main@5fcc1ea45cfa91e069eba62bcb887813646364af`
(merge of PR #93, `feat(fiscal): plan deterministic Sobre batches`).

There is no pending governed increment currently open. PR #93 is part of the accepted baseline after exact-head validation, explicit human merge approval and successful post-merge validation on `main`.

This file is the current human-readable checkpoint for the eFactura brownfield modernization. It does not replace requirements, architecture, API contracts or numbered implementation records. Files under `documentation/blueprint-brownfield/` remain historical inspection/remediation evidence and must not be rewritten to make the original AS-IS observations look current.

## Current validated baseline

- Runtime target: `.NET 10`.
- SDK pinned by `global.json`: `10.0.400`.
- Dedicated CI runner: `efactura-ci-01` on machine `Elena`.
- Current workflow selector: `[self-hosted, linux, x64, efactura-ci]`.
- CI database services: PostgreSQL 16 and MySQL 8.4 as isolated disposable service containers.
- NuGet vulnerability gate blocks known direct/transitive vulnerable packages.
- Clean Architecture + Ports & Adapters remains mandatory.

The accepted `main` commit is the GitHub-verified merge of PR #93. Its post-merge Clean Architecture Guard #439, run `34782977033`, completed successfully on exact `main@5fcc1ea45cfa91e069eba62bcb887813646364af`, push event, attempt 1, with no rerun. NuGet vulnerability gating passed; Build completed with **0 errors**; ArchitectureTests completed **185/185 passed**, CrossCuttingTests **321/321 passed**, legacy UnitTest **21/21 passed**, and the PostgreSQL/MySQL provider-real suite completed **221/221 passed**. The provider-real run used PostgreSQL 16.15 and MySQL 8.4.11; container setup and teardown passed.

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

Detailed bounded evidence remains under `documentation/blueprint-api-implementation/` through accepted implementation record `63_FISCAL_SOBRE_BATCH_PLANNING.md`.

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

## Accepted Sobre boundary after PR #93

The accepted CFE Sobre path now reaches:

```text
durable signed CFE artifacts
-> explicit caller-selected CFE candidate sequence
-> deterministic local Sobre batch planning
-> exact certificate thumbprint + serial-number grouping
-> first-seen certificate-group ordering
-> caller order preserved inside each certificate group
-> max 250 CFE per batch
-> local BatchOrdinal only
-> each planned batch enters the existing packaging boundary
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
-> signature-math row keeps CertificateTrustValidated = false
-> exact embedded-certificate SHA-256 continuity into trust validation
-> externally configured and SHA-256-pinned PKI Uruguay roots/intermediates
-> X509Chain CustomRootTrust
-> online revocation for EntireChain
-> append-only PKI Uruguay trust evidence
-> PkiUruguayTrustValidated = true
-> DgiIdentityValidated = false
-> provider-real trust replay/convergence
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
- records `CertificateTrustValidated = false` explicitly on the signature-verification row;
- uses `Restrict` FKs to ACK observation, submission and Sobre;
- replays/converges provider-real without rewriting any accepted source evidence.

The detailed accepted evidence is recorded in:
`documentation/blueprint-api-implementation/60_FISCAL_SOBRE_ACK_SIGNATURE_VERIFICATION.md`.

This accepted boundary validates signature mathematics and whole-document coverage only. PR #91 adds certificate-chain trust as a separate append-only record; the historical PR #87 verification row is not rewritten.

## Accepted PR #89 CFE-state consultation boundary

PR #89 adds a bounded **read-only CFE-state consultation** through the authoritative DGI `ws_consultas / EFACCONSULTARESTADOCFE` contract for an already durable local fiscal identity.

The accepted capability:

- accepts the local `FiscalDocumentId`, then reads the immutable `TipoCFE + Serie + Nro` identity from durable fiscal evidence;
- performs the DGI consultation only through an Infrastructure WS-Security adapter;
- keeps endpoint and SOAPAction externally configured;
- parses `Ackconsultaestadocfe` fail-closed;
- preserves raw `EstadoCFE`, `IdEmisor`, `IdReceptor` and optional `ParamConsulta(Token, Fechahora)` evidence;
- treats `Token` and `Fechahora` as an all-or-nothing evidence pair;
- stores the exact response XML and SHA-256 append-only;
- uses a `Restrict` FK to the source `FiscalDocument`;
- enforces operation replay by `OrganizationId + OperationId`;
- preserves the source `FiscalDocument`, sale, accounting, inventory, Sobre and ACK evidence unchanged;
- does not invent semantic meanings for `EstadoCFE` values;
- does not invoke the returned token or claim the separately documented `ID Receptor + Token` second-response lifecycle has been implemented.

The detailed accepted evidence is recorded in:
`documentation/blueprint-api-implementation/61_FISCAL_CFE_STATE_CONSULTATION.md`.

This accepted boundary is deliberately distinct from the token-based `Consulta de Comprobantes` described by DGI response-format documentation. The reviewed `Servicios Web Externos DGI` v1.9 catalog proves `EFACCONSULTARESTADOCFE` with `TipoCFE + Serie + Nro` input, but does not prove a token-input operation for the second/document-level CFE response.

## Accepted PR #91 ACKSobre PKI Uruguay certificate-trust boundary

PR #91 adds bounded **append-only PKI Uruguay certificate-trust validation** after the accepted ACKSobre XMLDSig signature-math verification.

The accepted capability:

- performs no new DGI network call;
- requires an already accepted ACKSobre signature-verification record;
- re-extracts the embedded certificate from the exact durable ACK response XML and requires SHA-256 continuity with the certificate evidence accepted by PR #87;
- keeps signature mathematics separate from trust-chain evaluation;
- validates with .NET `X509Chain` using `X509ChainTrustMode.CustomRootTrust`;
- requires public CA roots/intermediates to be supplied through external absolute paths with explicit SHA-256 pins rather than hardcoded repository trust material;
- requires online revocation checking with `X509RevocationFlag.EntireChain` and `X509VerificationFlags.NoFlag`;
- requires the end-entity certificate to allow digital-signature usage;
- persists successful trust evidence append-only and protects operation replay by `OrganizationId + OperationId`;
- converges concurrent same-operation validation on both PostgreSQL and MySQL;
- preserves the ACK observation, submission, Sobre and historical signature-verification evidence unchanged;
- records `PkiUruguayTrustValidated = true` while keeping `DgiIdentityValidated = false` explicitly.

The implementation evidence introduced by PR #91 is recorded in:
`documentation/blueprint-api-implementation/62_FISCAL_SOBRE_ACK_CERTIFICATE_TRUST.md`.

Its historical document status remains `GOVERNED IMPLEMENTATION CANDIDATE`; this checkpoint reconciliation does not rewrite numbered historical evidence. A valid PKI Uruguay chain plus online revocation does not by itself prove DGI legal signer identity, DGI-specific end-entity habilitation, or a DGI-specific OCSP/CRL endpoint policy.

## Accepted PR #93 deterministic Sobre batch-planning boundary

PR #93 adds a bounded local **product policy** before the existing Sobre packaging boundary.

The accepted capability is deliberately constrained to **caller-selected CFE only**:

- the caller supplies an explicit ordered set of `FiscalDocumentId` values;
- every selected CFE must already have a durable signed artifact;
- duplicate fiscal-document ids are rejected before repository reads;
- candidates are grouped by the exact durable certificate thumbprint + serial-number pair;
- certificate groups use first-seen certificate-group ordering from the caller input;
- caller order preserved inside each certificate group;
- each certificate group is split deterministically with max 250 CFE per batch;
- returned `BatchOrdinal` values are local-only and have no DGI meaning;
- the same input and unchanged durable certificate evidence produce the same ordered plan.

The accepted planner explicitly **does not discover pending CFE**, does not choose business/fiscal eligibility, does not allocate `Idemisor`, does not create or reserve a DGI Sobre identity, does not generate `EnvioCFE` XML, does not persist a Sobre or batch plan, does not call DGI, does not retry transport, does not interpret ACKSobre, `EstadoCFE` or S08, and does not mutate sale, accounting, inventory or fiscal-document state.

Each returned batch must still pass through the previously accepted packaging, durable identity and transport boundaries. DGI constraints already accepted by the repository remain unchanged: one Sobre contains `1..250` CFE/CFC and all CFE in the same Sobre use the same certificate. No additional DGI batching algorithm is inferred from those constraints.

The implementation evidence introduced by PR #93 is recorded in:
`documentation/blueprint-api-implementation/63_FISCAL_SOBRE_BATCH_PLANNING.md`.

Its historical document status remains `GOVERNED IMPLEMENTATION CANDIDATE`; this checkpoint reconciliation records the accepted post-merge state without rewriting that historical implementation record.

Formal traditional DGI Testing readiness remains exactly:

**BLOCKED BY MISSING PRODUCT CAPABILITIES**

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
- `Servicios Web Externos DGI`, code `T-5.020.00.001-000005`, version `1.9`, dated `13/05/2024`, for current consultation methods, including the accepted `EFACCONSULTARESTADOCFE` request identity `TipoCFE + Serie + Nro`;
- DGI material tying recognized eFactura certificates to certification providers accredited before UCE;
- UCE evidence defining ACRN as the PKI Uruguay root of trust, publishing the national trust-list boundary and requiring relying parties to validate certificate validity/revocation;
- DGI FAQ v22 section 9.7 for the explicit Reporte Diario AR/BR/DR/ER/FR meanings and the accepted reconciliation-policy boundary;
- accepted DGI Sobre constraints that one Sobre contains `1..250` CFE/CFC and all CFE in one Sobre use the same certificate.

The accepted PKI Uruguay trust boundary proves chain/revocation validation against explicitly pinned external trust material. It does not prove that the ACK end-entity certificate is legally authorized by DGI for a particular identity or role, so `DgiIdentityValidated = false` remains mandatory.

The accepted `EFACCONSULTARESTADOCFE` boundary can return Sobre consultation parameters as evidence for the first Sobre received by DGI for the queried CFE. The reviewed authoritative material still does not establish a web-service operation whose input is the ACKSobre `Token` and whose output is the second/document-level CFE response. That capability remains fail-closed rather than invented.

The accepted local batch planner is a consumer product policy only. The DGI 1..250 and same-certificate constraints do not establish automatic candidate discovery, fiscal eligibility selection, `Idemisor` allocation, pending ordering, retry behavior or any additional wire semantics.

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
- DGI-specific legal signer identity/certificate habilitation policy beyond accepted PKI Uruguay chain and online revocation validation;
- automatic `Idemisor` allocation/reservation;
- automatic pending-CFE discovery or business/fiscal eligibility selection for Sobre batch planning;
- persisted batch-plan lifecycle or automatic conversion of the local batching policy into a pending-work selector;
- authoritative semantic interpretation of the returned `EstadoCFE` taxonomy and any resulting local lifecycle transition;
- document-level CFE response consultation/interpretation after an AS token because no governed token-input operation has been proven;
- `S08` interpretation/recovery semantics beyond preserving the reason;
- automatic reconciliation of ambiguous Sobre `Unknown` delivery;
- authoritative external DGI Testing evidence;
- Production DGI/provider transport enablement;
- production HSM/Key Vault custody;
- remaining public fiscal/status/cancellation APIs;
- general receivable collection/payment allocation workflow;
- accounts payable/procurement/treasury/cash-management completion.

## Blueprint evaluator checkpoint

Accepted historical eFactura evidence remains governed by its recorded evaluator versions and must not be rewritten retroactively.

Blueprint Master was last reverified for this lineage at `737556e24195aa909117790f2d7ff0be2fe0a474`, with root `VERSION = 0.5.2` and annotated tag `v0.5.2` resolving to that same commit.

There is no automatic consumer upgrade. Current consumer classification remains **DEFER formal 0.5.2 adoption** until its separate runtime/label migration items are deliberately executed and approved.

## Next bounded implementation sequence

PR #93 is closed and accepted. Later CFE response interpretation, DGI-specific identity, recovery and product-capability work may advance only through separately governed, evidence-backed increments.

The accepted PR #93 boundary now provides deterministic local batching of explicitly caller-selected, already-signed CFE. It respects the accepted same-certificate and 250-CFE envelope constraints while deliberately stopping before candidate discovery, `Idemisor`, XML packaging, persistence, transport and response semantics.

The accepted PR #91 boundary continues to validate the already verified ACKSobre certificate against explicitly pinned external PKI Uruguay trust material with online entire-chain revocation checking. It deliberately stops at `PkiUruguayTrustValidated = true` and `DgiIdentityValidated = false`; no DGI-specific end-entity identity or habilitation rule is inferred.

The authoritative revalidation accepted `EFACCONSULTARESTADOCFE` as a read-only query by durable `TipoCFE + Serie + Nro`, including raw `EstadoCFE`, `IdEmisor`, `IdReceptor` and optional Sobre consultation parameters. It did not establish an authoritative taxonomy that permits local lifecycle transitions from `EstadoCFE`.

The same revalidation continues to show that the ACKSobre token is documented as consultation evidence, while the current WS Consultas v1.9 contract does not prove a token-input operation for obtaining the second CFE response. That path therefore remains fail-closed.

DGI defines `Idemisor` as a number assigned by the issuer, but the reviewed material does not provide an authoritative algorithm for choosing the next value. Automatic allocation therefore remains out of scope instead of being guessed.

The Reporte Diario revalidation also found no governed WS Consultas v1.9 method that authoritatively retrieves ER inconsistency details and no authoritative R05 mechanism that returns the correct next sequence. Those capabilities remain fail-closed.

Candidate boundaries after PR #93 include:

1. DGI-specific legal signer identity/certificate habilitation only if authoritative end-entity policy evidence is sufficient;
2. token-input document-level CFE response consultation only if DGI publishes a sufficient authoritative operation/input/output/correlation contract;
3. semantic interpretation of `EstadoCFE` only if an authoritative state taxonomy and safe local transition policy are proven;
4. reconciliation/discovery for ambiguous Sobre `Unknown` only if authoritative service evidence exists;
5. S08 recovery only if authoritative evidence proves a safe action rather than merely the rejection reason;
6. automatic `Idemisor` allocation only if sufficient authoritative evidence is found;
7. ER inconsistency-detail retrieval only if DGI exposes sufficient authoritative evidence;
8. separately evidenced Reporte Diario `R05` sequence recovery only if the correct sequence can be proven rather than guessed;
9. external DGI Testing evidence using legitimate credentials/certificate material outside source control;
10. Production transport only after explicit technical and operational review.

## Known non-blocking modernization debt

Green builds may still report advisory legacy debt including deprecated/outdated dependencies, Application Insights legacy APIs, old ASP.NET abstractions, provider/design packages, xUnit deprecation notices, nullable/analyzer warnings, obsolete cryptography APIs and Windows-only `System.Drawing` usage.

These items remain inventory for later bounded modernization slices and must not be upgraded wholesale without compatibility analysis.

## Repository governance at this checkpoint

- accepted `main`: `5fcc1ea45cfa91e069eba62bcb887813646364af`;
- accepted merge: PR #93 `feat(fiscal): plan deterministic Sobre batches`;
- approved PR #93 head: `1f8d901bf2acfd74536189b2517ef7992cebf38e`;
- exact-head PR #93 Clean Architecture Guard #438 (`34780560192`): SUCCESS;
- post-merge Clean Architecture Guard #439 (`34782977033`): SUCCESS, push on exact accepted merge SHA, attempt 1, no rerun;
- no governed increment is currently open;
- one atomic slice per PR remains required;
- Blueprint 0.5.2 consumer adoption remains DEFER;
- merge requires final exact-head green CI and explicit human approval.
