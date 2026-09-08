# Blueprint Current State

Status: CURRENT HUMAN CHECKPOINT

Checkpoint date: 2026-09-08

Accepted functional baseline: `main@c080fa7b298f971ad4490a580bb0d44c6bd8b009`
(merge of PR #44, `feat(fiscal): establish fiscal document identity foundation`).

This file is the current human-readable checkpoint for the eFactura brownfield modernization.
It does not replace requirements, architecture, API-contract or implementation records.
Files under `documentation/blueprint-brownfield/` remain historical inspection/remediation
evidence and must not be rewritten to make the original AS-IS observations look current.

## Current validated baseline

- Runtime target: `.NET 10`.
- SDK pinned by `global.json`: `10.0.400`.
- Dedicated CI runner: `efactura-ci-01` on machine `Elena`.
- Current workflow selector: `[self-hosted, linux, x64, efactura-ci]`.
- CI database services: PostgreSQL 16 and MySQL 8.4 as isolated disposable service containers.
- NuGet vulnerability gate blocks known direct/transitive vulnerable packages.
- Deprecated/outdated package inventories remain advisory modernization evidence.

## Post-merge evidence

`Clean Architecture Guard` run #174 (`34251929111`) validated the accepted merge commit
`c080fa7b298f971ad4490a580bb0d44c6bd8b009` after PR #44 merged:

- runner: `efactura-ci-01` on `Elena`;
- .NET SDK: `10.0.400`;
- .NET runtime observed: `10.0.11`;
- restore: PASS;
- NuGet known-vulnerability gate: PASS, 0 known vulnerable packages across all 10 projects;
- Release build: PASS, 89 warnings, 0 errors;
- ArchitectureTests: 70/70 PASS;
- CrossCuttingTests: 64/64 PASS;
- legacy UnitTest: 21/21 PASS;
- PersistenceIntegrationTests: 127/127 PASS on PostgreSQL 16 and MySQL 8.4;
- total represented automated tests: 282/282 PASS;
- 5-minute per-test `blame-hang` guard did not trigger.

The warnings remain advisory legacy/modernization debt and are not security-gate failures.

## Implemented and accepted v1 boundaries

Key accepted slices now include:

1. Sales draft + validation + fiscal preview.
2. Inventory availability + stock adjustment.
3. CAE authorization/allocation + atomic fiscal-number reservation.
4. Runtime/CI modernization to .NET 10.
5. Dependency/security modernization.
6. Authoritative Release-1 tax treatment, VAT/CFE eligibility and CFE 25.2 arithmetic foundations.
7. Sale confirmation planning with server-owned regulatory/inventory evidence.
8. Sale settlement planning.
9. Payment/Receivable persistence foundation.
10. Atomic tracked-stock and durable fiscalization local effects.
11. One atomic local `ConfirmSaleUseCase`.
12. Public `API-SAL-007 confirmSale`, merged by PR #42.
13. Fiscal Document Identity Foundation, merged by PR #44.

Detailed implementation evidence is maintained through file 24 under
`documentation/blueprint-api-implementation/`.

## Cross-cutting capabilities now present

For the new v1 write path, accepted slices include:

- Clean Architecture guards;
- provider-neutral transactional persistence;
- durable audit and outbox evidence;
- actor/correlation foundations;
- idempotency/replay protection;
- optimistic and unique concurrency guards;
- PostgreSQL/MySQL integration coverage;
- authoritative sale confirmation with Payment/Receivable, tracked inventory and one durable
  `FiscalizationRequest`;
- deterministic fiscalization retry identity based on `FiscalizationRequestId`;
- atomic CAE-backed number reservation plus immutable `FiscalDocument` identity/snapshot;
- rollback of number reservation, fiscal document, workflow state, audit and outbox as one local
  transaction.

These capabilities do not imply that every legacy endpoint has been migrated to the same standards.

## Accepted fiscal boundary after PR #44

The accepted fiscal flow now reaches:

`Sale CONFIRMED`
`-> FiscalizationRequest PENDING`
`-> CAE number reserved`
`-> FiscalDocument IDENTITY_CREATED`

The `FiscalDocument` identity snapshot owns server-assigned fiscal identity and accepted evidence,
including CFE type, series/number, CAE provenance, fiscal date, format version, confirmation and
settlement fingerprints and authoritative monetary totals.

It is not yet a complete signed CFE artifact.

## Explicitly not complete

The following remain outside the accepted current baseline:

- CFE XML generation;
- well-formedness/XSD validation against the active official DGI specification;
- XML digital signature;
- certificate/private-key custody;
- immutable signed-artifact archival;
- DGI or authorized-provider transport;
- direct-DGI-vs-provider production decision;
- synchronous receipt and asynchronous acceptance/rejection interpretation;
- fiscal regularization/retry lifecycle after external submission;
- `API-SAL-008 cancelSale`;
- `API-SAL-009 getSaleFiscalizationStatus`;
- public `API-FIS-*` document routes;
- credit/debit/correction notes;
- contingency/CFC lifecycle;
- daily fiscal reporting/homologation evidence;
- general receivable collection/payment allocation workflow;
- accounts payable/procurement/treasury/cash-management completion.

Regulatory decisions explicitly left open in accepted fiscal/tax documents remain open until
separately reviewed against current official evidence.

## Blueprint evaluator checkpoint

Accepted historical eFactura evidence remains governed by:

- Blueprint evaluator version: `0.5.1`;
- exact evaluator commit: `ac8be4e3332b13cab7d27f12e6a62d5d60e9ff4e`;
- annotated evaluator tag: `v0.5.1`.

Blueprint Master was reverified on 2026-09-08:

- repository: `LuisHdezE/SoftwareDevelopmentBlueprint`;
- current `main`: `737556e24195aa909117790f2d7ff0be2fe0a474`;
- root `VERSION`: `0.5.2`;
- annotated tag `v0.5.2` resolves to the same commit.

There is no automatic consumer upgrade from 0.5.1 to 0.5.2.

The dedicated consumer Compliance Review is recorded in:

`documentation/BLUEPRINT_0_5_2_CONSUMER_COMPLIANCE_REVIEW.md`

Current classification: **DEFER formal 0.5.2 adoption**.

Reason: Blueprint 0.5.2's self-hosted CI runtime contract requires the canonical `blueprint` runner
label and a versioned `.blueprint/ci-runtime.yaml` runtime artifact. eFactura currently selects
`[self-hosted, linux, x64, efactura-ci]` and does not yet contain the 0.5.2 runtime artifact.
Claiming compliance before that migration would be false.

The deferred runtime migration is governance/portability work, not a product blocker. Until a
separate adoption PR completes its MIGRATE items and receives explicit human approval, the consumer
evaluator remains `0.5.1@ac8be4e3332b13cab7d27f12e6a62d5d60e9ff4e`.

Historical files that correctly identify evaluator 0.5.1 remain historical evidence and must not be
rewritten merely to display the newer Master version.

## Next selected implementation slice

The next bounded fiscal implementation slice is selected as:

**CFE XML Artifact Foundation - BUILD + VALIDATE**

Target boundary:

`FiscalDocument IDENTITY_CREATED -> CFE BUILT -> CFE VALIDATED`

The architecture already defines the intended Application ports `IFiscalXmlBuilder` and
`IFiscalValidator`. The slice must stop before `IFiscalSigner`, certificate/private-key custody,
immutable signed-artifact archival and `IFiscalTransportGateway`.

Before implementation, current official DGI evidence must be collected for the active CFE 25.2 XML
structures/XSDs and the Release-1 CFE families already bounded by this project. XML elements,
namespaces, mandatory fields, schema rules and signing requirements must not be invented from
memory, legacy demo code or provider-specific examples.

## Known non-blocking modernization debt

The current build is green but still reports legacy/advisory debt, including deprecated/outdated
dependencies, Application Insights legacy APIs, `Microsoft.AspNetCore.Http.Abstractions 2.2.0`,
legacy Npgsql extension/design packages, xUnit 2.x deprecation notices, nullable/analyzer warnings,
obsolete cryptography APIs and Windows-only `System.Drawing` usage.

GitHub Actions also reports Node 20 deprecation warnings for action versions that are currently being
forced to execute on Node 24.

These items are inventory for later bounded slices. They must not be upgraded wholesale without
compatibility analysis.

## Documentation interpretation

- `documentation/blueprint-brownfield/01..12`: historical AS-IS, gap and remediation artifacts.
- `documentation/blueprint-api-implementation/`: implementation evidence by bounded slice.
- `documentation/BLUEPRINT_0_5_2_CONSUMER_COMPLIANCE_REVIEW.md`: consumer evaluator/version review.
- this file: current accepted operational checkpoint.

Where an old gap matrix conflicts with merged implementation evidence, the historical row remains
useful as provenance, while this checkpoint plus later accepted implementation records describe
current repository reality.

## Repository governance at this checkpoint

- PR #44 is merged.
- accepted `main`: `c080fa7b298f971ad4490a580bb0d44c6bd8b009`.
- post-merge Clean Architecture Guard #174: SUCCESS, 282/282 tests PASS.
- the Blueprint 0.5.2 adoption decision is DEFER, not ADOPT.
- local `qa/` / Postman work is not represented as an accepted repository gate here.
- the next product slice still requires a bounded branch/commit, exact-head validation and explicit
  human approval before merge.
