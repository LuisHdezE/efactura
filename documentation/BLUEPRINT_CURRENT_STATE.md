# Blueprint Current State — eFactura

Status: CURRENT HUMAN CHECKPOINT

Checkpoint date: 2026-09-08

Accepted functional baseline: `main@5eee259362ef44c025fca59e39a9ac7c5f22adb5`
(merge of PR #42, `feat(sales): expose sale confirmation API`).

This file is the current human-readable checkpoint for the eFactura brownfield modernization.
It does not replace requirements, architecture, API-contract or implementation records.
Files under `documentation/blueprint-brownfield/` remain historical inspection/remediation
evidence and must not be rewritten to make the original AS-IS observations look current.

## Current validated baseline

- Runtime target: `.NET 10`.
- SDK pinned by `global.json`: `10.0.400`.
- CI runner: dedicated self-hosted `efactura-ci-01`.
- CI database services: PostgreSQL 16 and MySQL 8.4 as isolated disposable service containers.
- NuGet vulnerability gate blocks known direct/transitive vulnerable packages.
- Deprecated/outdated package inventories remain advisory modernization evidence.

Exact-head `Clean Architecture Guard` run #168 validated the candidate tree merged by PR #42:

- restore: PASS;
- NuGet known-vulnerability gate: PASS, 0 known vulnerable packages across all 10 projects;
- Release build: PASS, 0 errors;
- ArchitectureTests: 64/64 PASS;
- CrossCuttingTests: 61/61 PASS;
- legacy UnitTest: 21/21 PASS;
- PersistenceIntegrationTests: 121/121 PASS on PostgreSQL 16 and MySQL 8.4;
- total represented automated tests: 267/267 PASS;
- 5-minute per-test `blame-hang` guard did not trigger.

## Implemented and accepted v1 boundaries

The accepted sequence has advanced beyond the earlier PR #33 checkpoint.

Key merged slices now include:

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

Detailed implementation evidence is maintained through file 23 under
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
  `FiscalizationRequest`.

These capabilities do not imply that every legacy endpoint has been migrated to the same standards.

## Fiscal boundary after PR #42

A confirmed sale now ends its local transaction at:

`Sale CONFIRMED -> FiscalizationRequest PENDING`

The accepted code still stops before:

- consuming the fiscalization work item;
- reserving a CAE number as part of that workflow;
- creating FiscalDocument identity/snapshot;
- CFE XML generation/XSD validation;
- XML signing;
- certificate/private-key custody;
- immutable signed-artifact persistence;
- DGI/provider transport and response interpretation.

The next implementation record, file 24, describes the candidate
**Fiscal Document Identity Foundation**. It is not an accepted baseline until its own PR is
exact-head validated and explicitly approved for merge.

## Explicitly not complete

The following remain outside the accepted current baseline:

- FiscalDocument identity and lifecycle;
- CFE XML generation/validation/signing;
- certificate/private-key custody;
- DGI or provider transport;
- direct-DGI-vs-provider production decision;
- `API-SAL-008 cancelSale`;
- `API-SAL-009 getSaleFiscalizationStatus`;
- public `API-FIS-*` document routes;
- credit/debit/correction notes;
- contingency lifecycle;
- DGI receipt/async acceptance/rejection and regularization workflow;
- daily fiscal reporting/homologation evidence;
- general receivable collection/payment allocation workflow;
- accounts payable/procurement/treasury/cash-management completion.

Regulatory decisions explicitly left open in accepted fiscal/tax documents remain open until
separately reviewed against current official evidence.

## Known non-blocking modernization debt

The current build is green but still reports legacy/advisory debt, including deprecated or
outdated dependencies, Application Insights legacy APIs, `Microsoft.AspNetCore.Http.Abstractions
2.2.0`, legacy Npgsql extension/design packages, xUnit 2.x deprecation notices,
nullable/analyzer warnings, obsolete cryptography APIs and Windows-only `System.Drawing` usage.

These items are inventory for later bounded slices. They are not known-vulnerability gate
failures and must not be upgraded wholesale without compatibility analysis.

## Documentation interpretation

- `documentation/blueprint-brownfield/01..12`: historical AS-IS, gap and remediation artifacts.
- `documentation/blueprint-api-implementation/`: implementation evidence by bounded slice.
- this file: current accepted operational checkpoint plus explicit notice of the next unaccepted
  candidate.

Where an old gap matrix conflicts with merged implementation evidence, the historical row remains
useful as provenance but this checkpoint plus later implementation records describe current reality.

## Repository governance at this checkpoint

- PR #42 is merged.
- `main` is `5eee259362ef44c025fca59e39a9ac7c5f22adb5`.
- no open pull requests were present when the next slice was selected.
- local `qa/` / Postman work is not represented as an accepted repository gate here.
- the next slice still requires one bounded branch/commit, exact-head CI and explicit human approval
  before merge.
