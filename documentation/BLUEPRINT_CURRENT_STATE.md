# Blueprint Current State — eFactura

Status: CURRENT HUMAN CHECKPOINT

Checkpoint date: 2026-09-03

Canonical repository state: `main@0d6b68753700e8dfcb98ced03882e821fe76e252`

This file is the current human-readable checkpoint for the eFactura brownfield modernization. It does not replace the detailed requirements, architecture, API-contract or implementation documents. Files under `documentation/blueprint-brownfield/` remain historical inspection/remediation evidence and must not be rewritten to make the original AS-IS observations look current.

## Current validated baseline

- Runtime target: `.NET 10`.
- SDK pinned by `global.json`: `10.0.400`.
- CI runner: dedicated self-hosted `efactura-ci-01` with labels `self-hosted`, `linux`, `x64`, `efactura-ci`.
- CI database services: PostgreSQL 16 and MySQL 8.4, isolated as disposable Docker service containers.
- CI service data is ephemeral and uses bounded `tmpfs` mounts to avoid WSL filesystem sync bottlenecks.
- NuGet vulnerability gate blocks known direct/transitive vulnerable packages.
- Deprecated/outdated package inventories remain advisory follow-up evidence.

## Post-merge evidence

`Clean Architecture Guard` run #136 validated the actual merged `main` at `0d6b68753700e8dfcb98ced03882e821fe76e252`:

- restore: PASS;
- NuGet known-vulnerability gate: PASS, 0 known vulnerable packages across all 10 projects;
- Release build: PASS, 0 errors;
- ArchitectureTests: 40/40 PASS;
- CrossCuttingTests: 16/16 PASS;
- legacy UnitTest: 21/21 PASS;
- PersistenceIntegrationTests: 93/93 PASS on PostgreSQL 16 and MySQL 8.4;
- total represented automated tests: 170/170 PASS;
- persistence suite runtime: 33.271 seconds;
- 5-minute per-test `blame-hang` guard did not trigger.

## Implemented and accepted v1 boundaries

The implementation sequence documented through file 11 has since advanced through these merged slices:

1. Sales draft + validation + fiscal preview, PR #29.
2. Inventory availability + stock adjustment, PR #30.
3. CAE authorization/allocation + atomic fiscal-number reservation, PR #31.
4. Runtime and CI modernization to .NET 10, PR #32.
5. Dependency/security modernization and AutoMapper retirement, PR #33.

Detailed implementation records are maintained as files 12–16 under `documentation/blueprint-api-implementation/`.

## Cross-cutting capabilities now present

For the new v1 write path, accepted slices include executable Clean Architecture guards, provider-neutral transactional persistence, durable audit evidence, actor/correlation foundations, outbox evidence, idempotency/replay protection, optimistic/unique concurrency guards, and PostgreSQL/MySQL integration coverage.

These capabilities do not imply that every legacy endpoint has been migrated to the same standards.

## Explicitly not complete

The following remain outside the accepted current implementation and must not be inferred from the presence of Sales/Inventory/CAE preparation:

- final CFE issuance;
- authoritative final CFE arithmetic/rounding;
- CFE XML generation;
- XML signing;
- certificate/private-key custody;
- DGI or provider transport;
- direct-DGI-vs-provider production decision;
- `confirmSale` and final stock/fiscal effects;
- payments and accounts receivable/payable flows;
- correction notes;
- contingency lifecycle;
- daily fiscal reporting/homologation evidence.

Regulatory decisions explicitly left open in the accepted fiscal/tax documents remain open until separately reviewed against current official evidence.

## Known non-blocking modernization debt

The current build is green but still reports legacy/advisory debt, including deprecated or outdated dependencies, Application Insights legacy APIs, `Microsoft.AspNetCore.Http.Abstractions 2.2.0`, legacy Npgsql extension/design packages, xUnit 2.x deprecation notices, nullable/analyzer warnings, obsolete cryptography APIs and Windows-only `System.Drawing` usage.

These items are inventory for later bounded slices. They are not known-vulnerability gate failures and must not be upgraded wholesale without compatibility analysis.

## Documentation interpretation

- `documentation/blueprint-brownfield/01..12`: historical AS-IS, gap and remediation artifacts captured earlier in the modernization.
- `documentation/blueprint-api-implementation/`: accepted implementation evidence by slice.
- this file: current operational checkpoint until superseded by a later approved checkpoint.

Where an old gap matrix conflicts with accepted merged implementation evidence, the historical row remains useful as provenance but this checkpoint plus the later implementation record describes current repository reality.

## Repository governance at this checkpoint

- PR #33 is merged.
- `main` is `0d6b68753700e8dfcb98ced03882e821fe76e252`.
- post-merge CI #136 is SUCCESS.
- no open pull requests were present when this checkpoint branch was created.
- local `qa/` / Postman work is not represented as an accepted repository gate here.

Any next implementation slice still requires explicit scope selection, exact-head validation and human review before merge.
