# API Implementation 15 — .NET 10 Runtime and CI Baseline

Status: MERGED / VERIFIED_IN_MAIN

Merged PR: #32 `chore(dotnet): modernize eFactura to .NET 10`

## Purpose

Align the solution runtime, Docker baseline and repository validation path after the functional v1 slices without changing their business behavior.

## Accepted runtime baseline

- all solution and test projects target `net10.0`;
- `global.json` pins SDK `10.0.400`;
- Docker runtime is aligned to .NET 10 and HTTP port 8080;
- the legacy `ApplicationCore` nullable policy remains `disable` rather than being changed as an unrelated migration side effect.

## CI isolation

Repository CI runs on the dedicated eFactura self-hosted runner with labels:

`self-hosted`, `linux`, `x64`, `efactura-ci`

The eFactura workflow must not reuse or control the separate CUSA runner.

PostgreSQL 16 and MySQL 8.4 are created as disposable Docker service containers with dynamically assigned host ports. They are isolated from developer-local PostgreSQL/MySQL instances.

## Validation model

The workflow restores/builds the solution, executes Clean Architecture guards, API v1 cross-cutting tests, legacy unit tests and the dual-provider persistence integration suite.

At PR #32 acceptance the local and CI baseline was 170/170 PASS, including 93/93 persistence integration tests.

The current checkpoint records later post-merge verification on the hardened workflow after PR #33.

## Deliberate boundaries

The runtime migration did not authorize wholesale provider/package major upgrades or broad warning cleanup. Dependency/security remediation was intentionally split into the following bounded slice rather than mixed into this runtime migration.
