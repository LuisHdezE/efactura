# UI-CAE-001 — Inventory Entry

Status: `SPECIFICATION_READY / VISUAL_BASELINE_APPROVED / PLANNED_DISABLED`

## Identity

- WEB: `WEB-014`
- UI: `UI-CAE-001`
- Product area: `Fiscal`
- Name: `Administración de CAE`
- Reserved route candidate: `/cae`
- Navigation state: `PLANNED_DISABLED`
- Runtime mode: implementation pending

## Visual evidence

Approved baseline:

`UI-CAE-001 / v1-responsive-composite`

Approved artifact:

`ea4d59d5-0a58-451f-bd34-9af9b939819b`

Approved aspect ratio: `4:3`.

The approved artifact includes a primary desktop light-mode composition plus dark-mode, tablet and mobile responsive compositions inside the same image.

The earlier pre-revision generation `a0635f18-94eb-4230-9af3-c32e37d1f37f` is not approved authority.

## Governed purpose

Import, validate, monitor and operationally allocate CAE authorization ranges/subranges without violating company-wide fiscal-numbering uniqueness or transferring numbering authority to the client.

## Accepted roles

- fiscal administrator
- administrator
- auditor

## Requirements

`FR-050..FR-056`.

## API dependencies

- `API-CAE-001` list CAE authorizations
- `API-CAE-002` CAE authorization detail
- `API-CAE-003` import CAE authorization
- `API-CAE-004` activate CAE authorization
- `API-CAE-005` list CAE allocations
- `API-CAE-006` create CAE allocation
- `API-CAE-007` close CAE allocation

Current Wave 5 implementation evidence: all seven dependencies are `IMPLEMENTED` through `CaeAuthorizationsController`.

Read authority uses `fiscal.read`. Mutation authority uses `fiscal.manage_cae`.

## Contract limitation

The current authorization DTO includes identity, version, CFE type, authorization number, series, range, validity, status, verification/provenance, timestamps and optional alert code.

The current allocation DTO includes allocation identity/version, CAE id, location/terminal, range, status and created/closed timestamps.

No authoritative consumed percentage, remaining-number count or `NextNumber` is exposed by the current HTTP DTO. The WebApp must not invent these values.

## Current decision

The approved baseline is preserved, but `/cae` remains `PLANNED_DISABLED` until a dedicated implementation PR creates the responsive React surface and explicitly reconciles route/capability registration and HTTP integration.

This inventory entry does not authorize backend/API, Domain, Persistence, database or auth changes.