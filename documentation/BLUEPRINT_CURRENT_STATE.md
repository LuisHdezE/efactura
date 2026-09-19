# Blueprint Current State

Status: CURRENT HUMAN CHECKPOINT / D2.4 CLOSURE CANDIDATE

Checkpoint date: 2026-09-18

Current governed repository baseline entering D2.4:

`main@9e1b9b6758215aa875e32d5792f85e6655dd7e07`

Latest accepted product-capability baseline remains:

`main@319bf1ffd6a24b31de344169fbe43d98c973a9fe`

which is the merge of PR #108:

`feat(fiscal): compose explicit ACKCFE evidence cycle`

D1.2 and D2 changed configuration, deployment, public demo exposure and operational governance. They do not rewrite the accepted product/fiscal semantics of PR #108.

This file is the current human-readable checkpoint for the eFactura brownfield modernization. It does not replace requirements, architecture, API contracts, numbered fiscal implementation records or deployment evidence. Historical records under `documentation/blueprint-brownfield/` and `documentation/blueprint-api-implementation/` remain historical and must not be silently rewritten to make old observations appear current.

## 1. Current validated engineering baseline

- Runtime target: `.NET 10`.
- Clean Architecture + Ports & Adapters remains mandatory.
- Dedicated CI runner: `efactura-ci-01` on machine `Elena`.
- Governed runner selector: `[self-hosted, linux, x64, efactura-ci]`.
- CI database services: PostgreSQL and MySQL provider-real disposable service containers.
- NuGet vulnerability gate blocks known direct/transitive vulnerable packages.
- `main` remains the stable accepted line.
- one bounded atomic slice per PR remains the normal governance rule.
- merge requires exact-head green CI where applicable plus explicit human approval.

The current D2.4 increment is documentation/governance only. It adds no new business use case, public endpoint, persistence schema or fiscal transition.

## 2. Current repository/deployment checkpoint

Current repository baseline before D2.4 merge:

`9e1b9b6758215aa875e32d5792f85e6655dd7e07`

This is the merge of PR #148:

`docs(d2.2): close automated API deployment with accepted evidence`

PR #148 was documentation only and did not trigger a new API deployment.

Current accepted deployed backend source remains:

`7eed21f969486bd689b5abf2e9c37ba2462b00bd`

Current accepted Cloud Run revision:

`efactura-api-d22-7eed21f-46-1`

Stable public API URL:

`https://efactura-api-yblnutgx3q-ul.a.run.app`

Accepted immutable image digest:

`sha256:0fec87aa1700f24c3f54682f2ab5940af0782fcdf1ebae9629ba05b8fb3c77ac`

Accepted rollback revision:

`efactura-api-d21-swagger-01`

## 3. D1.2 — Secrets & Configuration Hardening

Status:

`CLOSED`

Accepted outcomes:

- sensitive JWT/PostgreSQL/BlobStorage values are not populated in version-controlled WebApi configuration;
- local sensitive values use .NET User Secrets;
- Cloud Run sensitive values use Google Secret Manager;
- historically exposed JWT signing material was rotated before public deployment;
- the WebApi container runs on Linux/.NET 10 and port `8080`;
- Cloud Run -> Neon PostgreSQL connectivity is proven;
- protected public API behavior is proven;
- Redis is not required for the accepted demo runtime path.

Accepted D1.2 merge:

PR #129 / `1bf26f57bc94fc57b5ef3f34815cd4781e832b87`

## 4. D2 — Repeatable Deployment & Demo Integration

### D2.1 — Controlled second API revision + public Swagger

Status:

`CLOSED / DEPLOYED / VERIFIED`

Accepted evidence includes:

- PR #133;
- Clean Architecture Guard #534 / `35180211947`: SUCCESS;
- merge `8cbc896d5be7a8ff9d3ebaf0e84bd3ca56580360`;
- Cloud Run revision `efactura-api-d21-swagger-01`;
- public Swagger/OpenAPI HTTP `200`;
- protected parties endpoint HTTP `401` without JWT;
- authenticated parties endpoint HTTP `200` with valid JWT + Neon.

D2.1 proved manual controlled rollout before automation.

### D2.2 — Reproducible API deployment

Status:

`CLOSED / AUTOMATED / VERIFIED`

Accepted deployment workflow:

`.github/workflows/deploy-api-demo.yml`

Accepted model:

`accepted main -> linux/amd64 build -> OIDC/WIF -> Artifact Registry -> immutable digest -> zero-traffic tagged canary -> canary smoke -> 100% promotion -> public smoke -> automatic rollback path if post-promotion acceptance fails`

Accepted first automated deployment:

- run #46 / id `35406809175`;
- source `7eed21f969486bd689b5abf2e9c37ba2462b00bd`;
- conclusion `SUCCESS`;
- revision `efactura-api-d22-7eed21f-46-1`;
- canary smoke `200 / 200 / 401 / 200`;
- public post-promotion smoke `200 / 200 / 401 / 200`;
- final traffic 100% to accepted revision;
- rollback revision `efactura-api-d21-swagger-01` retained.

WIF/OIDC authentication is accepted without a long-lived Google service-account JSON key.

Deployment service account:

`efactura-deploy@efactura-demo-0916-9b93.iam.gserviceaccount.com`

Runtime service account:

`efactura-run@efactura-demo-0916-9b93.iam.gserviceaccount.com`

WIF provider:

`projects/195831190862/locations/global/workloadIdentityPools/github-actions/providers/efactura-main`

The provider trust is restricted to this repository identity and `refs/heads/main`.

### D2.3 — WebApp integration

Owner:

`parallel governed UI/WebApp lane`

The backend lane does not implement D2.3.

The stable public API URL plus Swagger/OpenAPI are the integration handoff surface.

### D2.4 — Backend operational closure

Status:

`CLOSURE CANDIDATE / GOVERNED REVIEW PENDING`

D2.4 reconciles:

- current accepted Cloud Run revision/image lineage;
- deployment/runtime service accounts;
- WIF provider/trust boundary;
- secret/configuration names without values;
- repeatable deployment procedure;
- automatic/manual rollback procedure;
- accepted public/canary smoke evidence;
- WebApp ownership boundary;
- known non-blocking operational debt;
- distinction between demo deployment readiness and product/API completeness.

Detailed D2.4 record:

`documentation/deployment/D2_4_BACKEND_OPERATIONAL_CLOSURE.md`

After exact-head Guard, explicit approval and accepted merge, the backend lane may be classified:

`D2 BACKEND LANE: READY_FOR_DEMO_INTEGRATION`

Full cross-lane D2 closure still depends on separately governed D2.3 work.

## 5. Current public backend contract boundary

The accepted demo API is publicly reachable at the Cloud Run network boundary while application authorization remains enforced by the WebApi.

Demo Swagger exposure is deliberate:

`Swagger:Enabled=false` by default

with demo Cloud Run setting:

`Swagger__Enabled=true`

Accepted D2.2 smoke behavior:

- `/swagger` -> final HTTP `200`;
- `/swagger/v1/swagger.json` -> HTTP `200`;
- unauthenticated `GET /api/v1/parties` -> HTTP `401`;
- authenticated `GET /api/v1/parties` -> HTTP `200` against Neon;
- JWT signing material does not appear in generated OpenAPI.

This proves the deployment/auth/persistence path for the accepted revision. It does not imply endpoint completeness.

## 6. Historical checkpoint continuity protected by architecture tests

The current operational checkpoint must preserve the accepted fiscal lineage rather than replacing it. The following historical facts remain part of the architecture contract.

PR #95 is part of the accepted baseline history. The earlier accepted checkpoint `main@8a70631cc5e2d723f88209459e5b77e487b66c20`, the merge of PR #95, remains the accepted source for token-input ACKCFE document-response consultation. At that checkpoint the repository recorded: `There is no pending governed increment currently open`. At that historical PR #95 checkpoint, cryptographic XMLDSig verification of returned `ACKCFE` remained an unresolved gap; PR #97 subsequently closed that signature-math gap, so this historical wording must not be read as the current capability state. That historical statement remains valid as lineage, not as the current product boundary.

The earlier accepted functional lineage also includes the following exact governed boundaries:

- Same-`SecEnvio` BR correction lineage with immutable local revisions and independent signing/schema evidence.
- Typed `R01..R06` BR reason evidence with `R05` fail-closed for separate sequence reconciliation.
- `SecEnvio N+1` authorization after durable AR on either the root N submission or an accepted same-`SecEnvio` correction for N.
- Authoritative original-response consultation for a known durable DGI `IdReceptor` through current `ws_consultas / EFACCONSULTARRESPUESTAREPORTE` evidence.
- Authoritative `EFACCONSULTARENVIOSREPORTE` receiver-id discovery for an explicit `Unknown` root/revision with no durable `IdReceptor`.
- Append-only observation of DGI Reporte Diario later states `DR`, `ER` and `FR` for an exact durable `IdReceptor`.
- Read-only reconciliation policy mapping `DR -> Consistent`, `ER -> ManualReviewRequired`, `FR -> ReliquidatedExternally`, always with automatic reliquidation and local mutation disabled and fail-closed timestamp ambiguity handling.
- Deterministic local Sobre v05 packaging for 1..250 already-signed CFE, same-certificate verification, signed-subtree preservation and byte-pinned `EnvioCFE.xsd` validation without persistence or transport.
- Durable local Sobre identity and replay persistence with explicit caller-supplied `Idemisor`, exact envelope/hash/schema/certificate evidence and provider-real PostgreSQL/MySQL concurrent replay convergence.
- Durable Sobre transport through `EFACRECEPCIONSOBRE`, with exact direct-CDATA `EnvioCFE`, WS-Security X509, `Prepared -> InFlight -> ResponseReceived|Unknown`, provider-real dispatch serialization and no automatic retry from `Unknown`.
- Append-only immediate `ACKSobre` observation mapping `AS -> Received` and `BS -> Rejected`, preserving DGI correlation ids, optional consultation parameters and S01..S08 evidence without CFE mutation or S08 recovery.
- Read-only authoritative DGI CFE-state consultation through `ws_consultas / EFACCONSULTARESTADOCFE` using the already durable `TipoCFE + Serie + Nro` identity, append-only exact response XML/SHA-256 evidence, provider-real replay protection and no mutation or invented `EstadoCFE` semantics.
- Append-only PKI Uruguay certificate-trust validation for an already verified ACKSobre signature, with exact embedded-certificate SHA-256 continuity, externally configured and SHA-256-pinned CA material, `X509Chain CustomRootTrust`, online revocation for EntireChain, provider-real replay/concurrency convergence, `PkiUruguayTrustValidated = true` and explicit `DgiIdentityValidated = false`.
- Deterministic local Sobre batch planning for explicitly caller-selected, already-signed CFE: exact certificate thumbprint + serial-number grouping, first-seen certificate-group ordering, caller order preserved inside each certificate group, max 250 CFE per batch and local-only `BatchOrdinal`, without pending-document discovery, `Idemisor` allocation, XML packaging, persistence, transport or DGI-state interpretation.
- Read-only ACKCFE document-response consultation by ACKSobre token through `WS_eFactura.EFACCONSULTARESTADOENVIO`, using an accepted durable `IdReceptor + Token`, exact response XML/SHA-256 evidence, strict Sobre/CFE correlation, partial-response support and append-only PostgreSQL/MySQL persistence without local lifecycle mutation or invented `ACKCFE_det/Estado` semantics.
- Append-only whole-document ACKCFE XMLDSig signature-math verification over the exact durable consultation response, with bounded algorithm/reference policy, embedded X.509 evidence, source SHA-256 continuity, provider-real replay/concurrency convergence and explicit `CertificateTrustValidated = false`, without PKI trust, DGI signer identity or state semantics.

For Reporte Diario receiver discovery, the accepted boundary never selects by timestamp or returned collection order. The accepted reconciliation safety flags remain explicit: `AutomaticReliquidationAuthorized = false` and `AutomaticLocalMutationAuthorized = false`.

For Sobre ACK observation, **S08 authorizes no automatic recovery**.

The DGI-specific legal signer identity/certificate habilitation policy remains unresolved and fail-closed.

## Accepted PR #87 ACKSobre signature-verification boundary

PR #87 adds bounded append-only ACKSobre XMLDSig cryptographic verification over the already durable ACK observation. It verifies signature mathematics and whole-document coverage only, requires an embedded X.509 certificate, keeps `CertificateTrustValidated = false`, and performs no new DGI network call. Certificate trust is a separate boundary.

The detailed accepted evidence remains in `documentation/blueprint-api-implementation/60_FISCAL_SOBRE_ACK_SIGNATURE_VERIFICATION.md`.

## Accepted PR #89 CFE-state consultation boundary

PR #89 adds a bounded read-only CFE-state consultation through the authoritative DGI `ws_consultas / EFACCONSULTARESTADOCFE` contract for an already durable local fiscal identity. It preserves raw `EstadoCFE`, `IdEmisor`, `IdReceptor` and optional `ParamConsulta(Token, Fechahora)` evidence and does not invent semantic meanings or local transitions.

The detailed accepted evidence remains in `documentation/blueprint-api-implementation/61_FISCAL_CFE_STATE_CONSULTATION.md`.

## Accepted PR #91 ACKSobre PKI Uruguay certificate-trust boundary

PR #91 adds bounded append-only PKI Uruguay certificate-trust validation after accepted ACKSobre signature-math verification. The adapter uses `X509Chain CustomRootTrust` with externally configured SHA-256-pinned trust material, online revocation for EntireChain, and exact embedded-certificate continuity. Successful evidence records `PkiUruguayTrustValidated = true` while `DgiIdentityValidated = false` remains mandatory.

The detailed accepted evidence remains in `documentation/blueprint-api-implementation/62_FISCAL_SOBRE_ACK_CERTIFICATE_TRUST.md`.

## Accepted PR #93 deterministic Sobre batch-planning boundary

PR #93 adds a bounded local product policy before packaging. The accepted capability is deliberately constrained to **caller-selected CFE only**, uses first-seen certificate-group ordering, keeps caller order preserved inside each certificate group and enforces max 250 CFE per batch. It does not discover pending CFE, does not allocate `Idemisor`, does not persist a batch plan, does not call DGI and does not interpret response semantics.

The detailed accepted evidence remains in `documentation/blueprint-api-implementation/63_FISCAL_SOBRE_BATCH_PLANNING.md`.

## Accepted PR #95 ACKCFE document-response consultation boundary

PR #95 adds a bounded read-only ACKCFE document-response consultation by ACKSobre token through `WS_eFactura.EFACCONSULTARESTADOENVIO`. The accepted capability requires durable Sobre/submission/AS evidence and a non-empty durable `IdReceptor + Token`, keeps endpoint/SOAPAction external, preserves exact ACKCFE XML plus SHA-256, strictly correlates Sobre and every returned CFE, permits partial responses, stores a hash of the source token, and preserves source evidence unchanged.

The detailed accepted evidence remains in `documentation/blueprint-api-implementation/64_FISCAL_CFE_DOCUMENT_RESPONSE_CONSULTATION.md`.

PR #95 does not itself validate the returned ACKCFE XMLDSig or PKI chain; PR #97 closed the XMLDSig signature-math portion of that historical gap and PR #101 later closed the separately governed PKI Uruguay chain-trust portion.

## 7. Current PR #108 functional boundary

PR #108 remains the latest accepted product-capability increment.

For one exact durable Sobre and one explicit caller-supplied `OperationId`, the accepted cycle composes:

1. `ConsultFiscalCfeEnvelopeDocumentResponseUseCase`;
2. `VerifyFiscalCfeEnvelopeDocumentResponseSignatureUseCase`;
3. `ValidateFiscalCfeEnvelopeDocumentResponseCertificateTrustUseCase`;
4. `AssessFiscalCfeEnvelopeDocumentResponseCoverageUseCase`.

The same normalized `OperationId` may replay/resume accepted durable checkpoints. A new operation id represents another explicit caller-requested consultation.

The accepted explicit safety flags remain:

- `DgiIdentityValidated = false`;
- `ProtocolFinalityProven = false`;
- `TokenExhaustionProven = false`;
- `AutomaticReconsultationAuthorized = false`.

D2 deployment work changes none of these semantics.

Detailed accepted evidence remains under `documentation/blueprint-api-implementation/` through `69_FISCAL_CFE_DOCUMENT_RESPONSE_EXPLICIT_EVIDENCE_CYCLE.md`.

## 8. DGI Testing readiness

Formal traditional `Prueba de Testing` readiness remains:

**BLOCKED BY MISSING PRODUCT CAPABILITIES**

External DGI Testing acceptance has not been established in this repository. Production enablement remains separately gated. Public Cloud Run demo readiness must not be represented as DGI certification or Production approval.

## 9. Explicitly incomplete product/business boundaries

The accepted baseline still does not complete, among other gaps:

- operational correction-note command/API integration into the normal sale workflow;
- export CFE families and export-specific immutable evidence;
- contingency/CFC lifecycle;
- authoritative BCU quotation acquisition and automatic exchange-rate rule selection;
- automatic Reporte Diario `R05` recovery semantics;
- ER inconsistency-detail retrieval/interpretation;
- governed human/operational approval for reliquidating `SecEnvio + 1`;
- local supersession/accounting mutation after observed `FR`;
- automatic retry from ambiguous Reporte Diario `Unknown`;
- DGI-specific legal signer identity/certificate habilitation beyond accepted PKI chain/revocation validation;
- automatic `Idemisor` allocation/reservation;
- automatic pending-CFE discovery/business eligibility selection for Sobre batching;
- persisted batch-plan lifecycle;
- authoritative semantic interpretation of returned `EstadoCFE` and any resulting local lifecycle transition;
- automatic ACKCFE token polling/reconsultation;
- token-exhaustion/protocol-finality proof;
- governed public REST exposure of the accepted explicit ACKCFE evidence cycle;
- local lifecycle transition from ACKCFE semantics/coverage;
- `S08` recovery semantics;
- automatic reconciliation of ambiguous Sobre `Unknown` delivery;
- authoritative external DGI Testing evidence;
- Production DGI/provider transport enablement;
- production HSM/Key Vault custody;
- remaining public fiscal/status/cancellation APIs;
- general receivable collection/payment allocation completion;
- accounts payable/procurement/treasury/cash-management completion.

These gaps are product/API work. They are not evidence that the D2 deployment platform is broken.

## 10. API completion after D2 backend closure

The repository is not endpoint-complete.

After accepted D2 backend closure, endpoint/API completion proceeds through the agreed wave model rather than being mixed into deployment work:

- Wave 1: Identity + Organization + Reference Data;
- Wave 2: Parties + Catalog + Sales Completion;
- Wave 3: Payments + Cash + AR/AP;
- Wave 4: Inventory + Transfers + Procurement + Receiving;
- Wave 5: Fiscal Completion + CAE + CFE Lifecycle;
- Wave 6: Reporting + Audit + Sync;
- Wave 7: Technical Operations Console.

Before implementing those waves, the API Completion Master Matrix must reconcile the authoritative contract inventory and resolve the known operation-count discrepancy rather than guess.

## 11. Blueprint evaluator checkpoint

Historical eFactura evidence remains governed by its recorded evaluator versions and is not retroactively rewritten.

Blueprint Master was last reverified for this lineage at `737556e24195aa909117790f2d7ff0be2fe0a474`, with root `VERSION = 0.5.2` and annotated tag `v0.5.2` resolving to that same commit.

There is no automatic consumer upgrade. Current consumer classification remains **DEFER formal 0.5.2 adoption** until its separate runtime/label migration items are deliberately executed and approved.

## 12. Known non-blocking modernization/operational debt

Green builds and accepted demo operation may still coexist with bounded technical debt including deprecated/outdated dependencies, Application Insights legacy APIs, old ASP.NET abstractions, provider/design packages, xUnit deprecation notices, nullable/analyzer warnings, obsolete cryptography APIs, Windows-only `System.Drawing` usage, Serilog Cloud Run stdout/stderr integration, the `UseHttpsRedirection()` internal container warning, custom demo domain and Redis absence where no concrete endpoint requires it.

These items remain future bounded modernization slices. They must not be upgraded wholesale without compatibility analysis.

## 13. Current governance lineage relevant to D2

Accepted functional product baseline:

- PR #108 `feat(fiscal): compose explicit ACKCFE evidence cycle`;
- merge `319bf1ffd6a24b31de344169fbe43d98c973a9fe`.

Deployment/configuration lineage:

- PR #129: D1.2 secrets/configuration hardening;
- PR #133: D2.1 public Swagger/revision;
- PR #139: D2.2 deployment workflow implementation;
- failed workflow run #15 / `35298178478`: YAML parsing/structure defect before cloud mutation;
- PR #142: bounded D2.2 workflow heredoc-indentation hotfix;
- exact-head Guard #551 / `35403102577`: SUCCESS;
- accepted deployment source/merge `7eed21f969486bd689b5abf2e9c37ba2462b00bd`;
- accepted deployment run #46 / `35406809175`: SUCCESS;
- PR #148: D2.2 documentation closure;
- exact-head manually dispatched Guard #554 / `35410181743`: SUCCESS;
- PR #148 merge `9e1b9b6758215aa875e32d5792f85e6655dd7e07`.

D2.4 now reconciles the backend operational checkpoint on top of that line.

## 14. D2.4 acceptance and next state

D2.4 is not accepted merely because this file is updated.

Acceptance requires:

1. D2.4 documentation changes on one exact branch head;
2. Clean Architecture Guard success on that exact head;
3. mergeability against the exact current `main` base;
4. Luis's explicit merge approval for that exact PR/head;
5. merge to `main`.

After accepted D2.4 merge:

`D2 BACKEND LANE: READY_FOR_DEMO_INTEGRATION`

This means the backend deployment platform is ready to support governed WebApp integration and later backend increments.

It does not mean D2.3 WebApp integration is already complete, all endpoints are implemented, DGI Testing is accepted or Production is enabled.

Full cross-lane D2 closure remains dependent on separately governed D2.3 work.

## 15. Next bounded backend sequence after D2.4

After D2.4 is accepted, the backend lane should:

1. create the API Completion Master Matrix from the authoritative contract/inventory;
2. reconcile the known ~193 vs 194 operation-count discrepancy before implementation;
3. preserve already implemented operations rather than duplicate them;
4. begin Wave 1 only after the matrix identifies exact missing operations and contracts;
5. continue one governed bounded increment per PR with exact-head CI and explicit merge approval.

Deployment work must no longer obscure product/API completion work.