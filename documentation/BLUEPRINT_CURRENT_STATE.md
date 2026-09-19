# Blueprint Current State

Status: CURRENT HUMAN CHECKPOINT / D2.4 CLOSURE CANDIDATE

Checkpoint date: 2026-09-18

Current governed repository baseline entering D2.4:

`main@9e1b9b6758215aa875e32d5792f85e6655dd7e07`

Latest accepted product-capability baseline remains:

`main@319bf1ffd6a24b31de344169fbe43d98c973a9fe`

which is the merge of PR #108:

`feat(fiscal): compose explicit ACKCFE evidence cycle`

D1.2 and D2 changed configuration, deployment, public demo exposure and operational governance. They do **not** rewrite the accepted product/fiscal semantics of PR #108.

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

## 6. Accepted product/fiscal functional baseline

PR #108 remains the latest accepted product-capability increment.

The accepted fiscal lineage includes the previously governed foundations for:

1. sales draft, validation and fiscal preview;
2. inventory availability and controlled stock adjustment;
3. CAE authorization/allocation and atomic fiscal-number reservation;
4. Release-1 tax treatment, VAT/CFE eligibility and CFE 25.2 arithmetic foundations;
5. sale confirmation and finance settlement foundations with atomic local effects;
6. fiscal document identity and organization fiscal issuer profile foundations;
7. immutable CFE content snapshots and frozen payment/unit-of-measure evidence;
8. deterministic unsigned CFE generation for Release-1 101/111 and bounded domestic correction-note foundation;
9. durable/replay-safe XMLDSig signing evidence and pinned DGI schema validation;
10. organization-scoped externally configured PFX composition with ephemeral key loading;
11. Reporte Diario v13.2 generation/signing/submission/evidence lifecycle;
12. provider-real serialization against duplicate network dispatch;
13. same-`SecEnvio` BR correction lineage and explicit R01..R06 evidence;
14. authoritative original-response consultation and receiver discovery for Reporte Diario;
15. append-only DR/ER/FR observations and read-only reconciliation policy;
16. deterministic local Sobre v05 packaging for already-signed CFE;
17. durable Sobre identity/replay persistence;
18. `EFACRECEPCIONSOBRE` transport with `Prepared -> InFlight -> ResponseReceived|Unknown`;
19. append-only immediate ACKSobre observation;
20. ACKSobre XMLDSig verification and separate PKI Uruguay trust validation;
21. authoritative read-only `EFACCONSULTARESTADOCFE` evidence capture;
22. deterministic caller-selected Sobre batch planning;
23. ACKCFE document-response consultation by accepted ACKSobre token;
24. ACKCFE whole-document XMLDSig verification;
25. ACKCFE PKI Uruguay certificate-trust validation;
26. authoritative ACKCFE detail-state interpretation for `AE`, `BE`, `CE` only;
27. trusted known-document coverage classification across durable ACKCFE observations;
28. explicit caller-triggered ACKCFE evidence-cycle composition.

Detailed accepted evidence remains under:

`documentation/blueprint-api-implementation/`

through record:

`69_FISCAL_CFE_DOCUMENT_RESPONSE_EXPLICIT_EVIDENCE_CYCLE.md`

## 7. PR #108 explicit ACKCFE evidence-cycle boundary

For one exact durable Sobre and one explicit caller-supplied `OperationId`, the accepted cycle composes:

1. `ConsultFiscalCfeEnvelopeDocumentResponseUseCase`;
2. `VerifyFiscalCfeEnvelopeDocumentResponseSignatureUseCase`;
3. `ValidateFiscalCfeEnvelopeDocumentResponseCertificateTrustUseCase`;
4. `AssessFiscalCfeEnvelopeDocumentResponseCoverageUseCase`.

The same normalized `OperationId` may replay/resume accepted durable checkpoints.

A new operation id represents another explicit caller-requested consultation.

The cycle contains no loop, polling worker, scheduler, retry cadence, maximum retry count, business timeout or local timestamp gate.

It does not claim global atomicity across the remote DGI call and later evidence writes.

It does not mutate sale, accounting, inventory or `FiscalDocument` state.

It does not establish DGI-specific signer identity/habilitation.

It exposes no governed public REST endpoint yet.

The accepted explicit safety flags remain:

- `DgiIdentityValidated = false`;
- `ProtocolFinalityProven = false`;
- `TokenExhaustionProven = false`;
- `AutomaticReconsultationAuthorized = false`.

D2 deployment work changes none of these semantics.

## 8. ACKCFE known-document coverage boundary

Accepted coverage classes remain:

- `NoDocumentCoverage`;
- `PartialDocumentCoverage`;
- `FullDocumentCoverage`.

Only XMLDSig-verified and PKI-trusted durable ACKCFE observations participate in trusted coverage assessment.

`FullDocumentCoverage` means every CFE identity in the durable Sobre is represented by non-contradictory currently known trusted evidence.

It does **not** prove:

- last response message;
- token exhaustion;
- protocol finality;
- automatic reconsultation authorization;
- local lifecycle transition.

## 9. DGI Testing readiness

Formal traditional `Prueba de Testing` readiness remains:

**BLOCKED BY MISSING PRODUCT CAPABILITIES**

External DGI Testing acceptance has not been established in this repository.

Production enablement remains separately gated.

Public Cloud Run demo readiness must not be represented as DGI certification or Production approval.

## 10. Explicitly incomplete product/business boundaries

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

## 11. API completion after D2 backend closure

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

## 12. Blueprint evaluator checkpoint

Historical eFactura evidence remains governed by its recorded evaluator versions and is not retroactively rewritten.

Blueprint Master was last reverified for this lineage at:

`737556e24195aa909117790f2d7ff0be2fe0a474`

with root:

`VERSION = 0.5.2`

and annotated tag:

`v0.5.2`

resolving to that same commit.

There is no automatic consumer upgrade.

Current consumer classification remains:

**DEFER formal 0.5.2 adoption**

until its separate runtime/label migration items are deliberately executed and approved.

## 13. Known non-blocking modernization/operational debt

Green builds and accepted demo operation may still coexist with bounded technical debt including:

- deprecated/outdated dependencies;
- Application Insights legacy APIs;
- old ASP.NET abstractions;
- provider/design packages;
- xUnit deprecation notices;
- nullable/analyzer warnings;
- obsolete cryptography APIs;
- Windows-only `System.Drawing` usage;
- Serilog Cloud Run stdout/stderr integration;
- `UseHttpsRedirection()` internal container warning;
- custom demo domain;
- Redis absence where no concrete endpoint requires it.

These items remain future bounded modernization slices. They must not be upgraded wholesale without compatibility analysis.

## 14. Current governance lineage relevant to D2

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

## 15. D2.4 acceptance and next state

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

It does **not** mean:

- D2.3 WebApp integration is already complete;
- all endpoints are implemented;
- DGI Testing is accepted;
- Production is enabled.

Full cross-lane D2 closure remains dependent on separately governed D2.3 work.

## 16. Next bounded backend sequence after D2.4

After D2.4 is accepted, the backend lane should:

1. create the API Completion Master Matrix from the authoritative contract/inventory;
2. reconcile the known ~193 vs 194 operation-count discrepancy before implementation;
3. preserve already implemented operations rather than duplicate them;
4. begin Wave 1 only after the matrix identifies exact missing operations and contracts;
5. continue one governed bounded increment per PR with exact-head CI and explicit merge approval.

Deployment work must no longer obscure product/API completion work. The platform is the runway; the next work is the aircraft.