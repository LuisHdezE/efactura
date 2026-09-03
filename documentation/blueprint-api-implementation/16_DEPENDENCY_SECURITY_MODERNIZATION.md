# API Implementation 16 — Dependency Security and CI Hardening

Status: MERGED / VERIFIED_ON_MAIN

Merged PR: #33 `chore(deps): harden dependency security and replace AutoMapper`

Merge commit: `0d6b68753700e8dfcb98ced03882e821fe76e252`

## Purpose

Harden the brownfield dependency graph after the .NET 10 migration while preserving API v1 business behavior, endpoint contracts and database schema.

## Dependency remediation

- legacy/infrastructure EF Core 8.x references were patched from 8.0.10 to 8.0.30 rather than mixed with an unrelated major-version migration;
- the vulnerable transitive Azure identity chain was remediated; the accepted graph resolved `Azure.Identity 1.12.1` and `Microsoft.Identity.Client 4.76.0`;
- `AutoMapper 12.0.1` and `AutoMapper.Extensions.Microsoft.DependencyInjection 12.0.1` were removed from the solution;
- `Mapster.DependencyInjection 10.0.12` was introduced only at the WebApi composition boundary;
- an explicitly temporary brownfield `AutoMapper.IMapper` compatibility contract plus outer adapter preserves legacy constructor compatibility while new v1 Domain/Application code remains forbidden from depending on it;
- the unused AutoMapper `ProjectTo` pagination helper was removed after confirming no production consumer.

## Security governance

CI now contains a blocking NuGet known-vulnerability gate covering direct and transitive dependencies across the solution. Deprecated and outdated inventories are advisory and intentionally separated from the blocking vulnerability decision.

The accepted post-merge run #136 reported 0 known vulnerable packages across all 10 projects.

## Runner/CI hardening

The investigation of earlier self-hosted-runner stalls produced bounded infrastructure changes rather than application database-timeout changes:

- solution restore is bounded and persistent .NET build servers are disabled on the relevant restore/build paths;
- PR validation and `push main` validation are separated to avoid duplicate same-repository runner competition;
- persistence collection concurrency is capped at two threads;
- each persistence test is protected by a 5-minute `blame-hang` guard without heavy dump generation;
- MySQL health retries tolerate slow cold startup;
- disposable PostgreSQL/MySQL data directories use bounded 1 GiB `tmpfs` mounts in CI.

The `tmpfs` change addresses high-churn temporary database I/O on WSL. It does not change production persistence durability or application provider behavior.

## Final verification

PR-head CI #135:

- ArchitectureTests 40/40 PASS;
- CrossCuttingTests 16/16 PASS;
- UnitTest 21/21 PASS;
- PersistenceIntegrationTests 93/93 PASS;
- total 170/170 PASS;
- persistence runtime 36.1092 seconds;
- known NuGet vulnerabilities: 0.

Post-merge `main` CI #136 repeated the gate on merge commit `0d6b68753700e8dfcb98ced03882e821fe76e252`:

- ArchitectureTests 40/40 PASS;
- CrossCuttingTests 16/16 PASS;
- UnitTest 21/21 PASS;
- PersistenceIntegrationTests 93/93 PASS;
- total 170/170 PASS;
- persistence runtime 33.271 seconds;
- Release build: 0 errors;
- known NuGet vulnerabilities: 0.

## Explicit non-goals and remaining debt

This slice does not claim that every package is current. Deprecated/outdated dependencies and legacy compiler/analyzer warnings remain bounded follow-up debt, including Application Insights legacy APIs, `Microsoft.AspNetCore.Http.Abstractions 2.2.0`, legacy Npgsql extension/design packages, xUnit 2.x notices, obsolete cryptography APIs and Windows-only `System.Drawing` usage.

Provider major-version upgrades remain separate compatibility work. No endpoint, business rule, persistence schema or production database timeout was changed to mask CI behavior.
