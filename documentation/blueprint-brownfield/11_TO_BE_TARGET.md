# TO-BE Target — Inspection Boundary

## Purpose

This file records only the high-level target implied by Brownfield gap analysis. Detailed product expansion is intentionally maintained on the separate branch `blueprint/target-functional-reconstruction` so reference/demo capabilities cannot contaminate AS-IS evidence.

## Direction

`eFactura hardened and expanded within its existing .NET ecosystem`.

High-level target principles:

- preserve .NET/C#/ASP.NET Core and clean dependency direction;
- remove secrets from committed configuration and establish secure credential/certificate custody;
- enforce explicit authentication/authorization policies;
- reconcile duplicate domain/persistence models using usage/evidence, not blind deletion;
- establish clear Domain/Application/Infrastructure/Web boundaries;
- define stable/versioned API/error contracts;
- add durable business/security audit separate from technical logs;
- introduce idempotency, correlation and safe external-integration lifecycle for financial/fiscal commands;
- strengthen automated architecture/API/persistence/security QA;
- align runtime/container/CI documentation with actual implementation.

## Separation from expanded product target

Capabilities such as POS, CFE/DGI, inventory, AR/AP, offline sync and specialized fiscal workflows are **PROPOSED TARGET/NEW REQUIREMENTS**, not current-system observations. They are not listed here as AS-IS deficiencies merely because a reference demo contains them.
