# Blueprint 0.5.2 Consumer Compliance Review — eFactura

Review date: 2026-09-08

## Purpose

Perform the required consumer-side Compliance Review before any evaluator change from Blueprint 0.5.1 to 0.5.2.

This review does not modify the Blueprint Master, product behavior, application architecture, fiscal rules or CI runner host. It exists to prevent an automatic or implicit consumer upgrade.

## Verified baselines

### eFactura consumer

- repository: `LuisHdezE/efactura`
- accepted consumer baseline: `main@c080fa7b298f971ad4490a580bb0d44c6bd8b009`
- baseline source: merge of PR #44, `feat(fiscal): establish fiscal document identity foundation`
- post-merge Clean Architecture Guard: run #174 (`34251929111`) — SUCCESS
- dedicated runner observed in CI: `efactura-ci-01` on machine `Elena`
- current workflow selector: `[self-hosted, linux, x64, efactura-ci]`

### Blueprint evaluator currently governing accepted historical evidence

- version: `0.5.1`
- exact commit: `ac8be4e3332b13cab7d27f12e6a62d5d60e9ff4e`
- annotated tag: `v0.5.1`

Historical Brownfield, Architecture and API Contract evidence that explicitly records evaluator 0.5.1 remains historical evidence and must not be rewritten merely because a newer Blueprint exists.

### Blueprint Master available for adoption review

- repository: `LuisHdezE/SoftwareDevelopmentBlueprint`
- current `main`: `737556e24195aa909117790f2d7ff0be2fe0a474`
- root `VERSION`: `0.5.2`
- annotated tag: `v0.5.2`
- tag resolves to: `737556e24195aa909117790f2d7ff0be2fe0a474`

Blueprint 0.5.2 is an execution-portability patch over 0.5.1. It does not add a phase, check, gate, client lifecycle state or product-specific rule. The new canonical concern is CI runtime governance.

## Canonical adoption rule

Blueprint 0.5.2 explicitly states that there is no automatic consumer upgrade. A consumer on 0.5.1 remains on 0.5.1 until it completes:

1. live verification of Master and consumer;
2. Compliance Review `0.5.1 -> 0.5.2`;
3. KEEP / ADOPT / MIGRATE / DEFER / N/A classification;
4. explicit human approval;
5. dedicated adoption PR;
6. impact-appropriate revalidation.

This document satisfies steps 1–3 only. It does not claim step 4 or later.

## Classification

### KEEP

The following accepted eFactura evidence and governance remain valid without semantic migration:

- Brownfield `ALIGN, DO NOT REWRITE` posture;
- Clean Architecture + Ports & Adapters requirement;
- existing phase/check/gate semantics inherited from 0.5.1;
- exact-candidate CI evidence requirement;
- human review and merge authorization remaining separate from CI success;
- historical 0.5.1 evaluator references in accepted Brownfield/Architecture/API Contract artifacts;
- dedicated repository-specific eFactura runner isolation from other projects;
- PostgreSQL/MySQL integration evidence and current product test scope.

### ADOPT

The following 0.5.2 semantics are compatible with current eFactura practice and should be adopted when formal migration occurs:

- CI evidence semantics are independent from runner ownership;
- a job that fails before runner assignment/execution is infrastructure evidence, not a product-test failure or PASS;
- repository-scoped self-hosted execution is appropriate for an isolated consumer lane;
- Actions service containers are runner infrastructure and do not make Docker an application/product requirement;
- exact-head/check-run evidence remains mandatory on self-hosted infrastructure.

### MIGRATE

Formal 0.5.2 adoption requires runtime-contract work that is not yet represented in the consumer repository:

1. add the canonical `blueprint` selector label to the dedicated eFactura self-hosted runner;
2. update `.github/workflows/clean-architecture.yml` so self-hosted jobs select both `blueprint` and the project-specific `efactura-ci` label;
3. materialize `.blueprint/ci-runtime.yaml` conforming to `schemas/ci-runtime.schema.json` 0.5.2;
4. record/prove the runtime security settings required by the contract, including trusted-code-only execution, fork PR handling, no persistent repository secrets and least-privilege permissions;
5. record workspace-cleanup and runner-update policy in the runtime artifact;
6. run impact-appropriate exact-head CI after the selector/runtime migration.

Current workflow evidence confirms selector `[self-hosted, linux, x64, efactura-ci]`; therefore claiming 0.5.2 compliance before the `blueprint` label migration would be false.

### DEFER

**Formal evaluator adoption to Blueprint 0.5.2 is DEFERRED.**

Until the MIGRATE items above are completed and explicitly approved, eFactura continues to use:

`Blueprint evaluator = 0.5.1@ac8be4e3332b13cab7d27f12e6a62d5d60e9ff4e`

This is a governance decision, not a product blocker. It prevents a silent evaluator switch while preserving all accepted product evidence.

### N/A

The following are not required by this patch:

- rewriting application/domain architecture;
- changing fiscal product scope;
- adding/removing Blueprint phases, checks or gates;
- containerizing the eFactura product;
- rewriting historical 0.5.1 evidence to display 0.5.2;
- changing PostgreSQL/MySQL application support merely because the runner uses service containers.

## Current product baseline after PR #44

Accepted fiscal workflow now reaches:

`Sale CONFIRMED -> FiscalizationRequest PENDING -> CAE number reserved -> FiscalDocument IDENTITY_CREATED`

The accepted system still stops before:

- CFE XML construction;
- official XSD validation;
- XML digital signature;
- certificate/private-key custody;
- immutable signed-artifact archival;
- DGI/provider transport;
- fiscal acceptance/rejection/regularization processing.

## Next selected implementation slice

The next bounded fiscal slice is selected as:

**CFE XML Artifact Foundation — BUILD + VALIDATE**

Target boundary:

`FiscalDocument IDENTITY_CREATED -> CFE BUILT -> CFE VALIDATED`

The slice must remain inside Domain/Application ports and Infrastructure adapters for XML/schema implementation. It must not include signing or transport.

Before code is accepted, the slice requires fresh official DGI evidence for the active CFE 25.2 XML structures/XSDs and the exact Release-1 document families already bounded by the project. No XML tag, namespace, mandatory field, schema rule or signature requirement may be invented from memory or from a demo implementation.

Architecture references already define the intended ports:

- `IFiscalXmlBuilder`;
- `IFiscalValidator`;
- later, but explicitly outside this slice: `IFiscalSigner`, `IFiscalArtifactStore`, `IFiscalTransportGateway`.

## Governance decision

This review does not authorize Blueprint 0.5.2 adoption.

The next product slice may continue under the accepted 0.5.1 evaluator while the runtime migration is separately deferred. A future dedicated 0.5.2 adoption PR must make the MIGRATE items concrete and receive explicit human approval before merge.
