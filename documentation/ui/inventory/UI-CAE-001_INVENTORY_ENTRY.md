# UI-CAE-001 — Inventory Entry

Status: `IMPLEMENTED_API_MOCK_DATA / VISUAL_BASELINE_APPROVED / RUNTIME_REVIEW_PENDING`

## Identity

- WEB: `WEB-014`
- UI: `UI-CAE-001`
- Product area: `Fiscal`
- Name: `Administración de CAE`
- Route: `/cae`
- Navigation state: `ACTIVE`
- Runtime mode: governed API operation map + mock gateway

## Visual evidence

Approved baseline: `UI-CAE-001 / v1-responsive-composite`.

- artifact gen_id: `ea4d59d5-0a58-451f-bd34-9af9b939819b`;
- aspect ratio: `4:3`;
- desktop light, dark, tablet and mobile responsive authority are present in the same approved composite.

The earlier generation `a0635f18-94eb-4230-9af3-c32e37d1f37f` is non-authoritative.

## Governed purpose

Inspect and administer CAE authorization ranges/subranges without violating fiscal-numbering authority or fabricating consumption state in the client.

## Requirements and roles

- requirements: `FR-050..FR-056`;
- roles: fiscal administrator, administrator, auditor.

## API dependencies

`API-CAE-001..007` are all `IMPLEMENTED` through `CaeAuthorizationsController`.

- reads: `fiscal.read`;
- mutations: `fiscal.manage_cae`.

The capability registers all seven operation IDs, but current WebApp API mode remains deliberately unavailable. Route activation therefore does not equal live HTTP integration.

## Implemented UI evidence

The implementation provides:

- responsive `CaePage` inside the shared shell;
- local/mock CAE and allocation gateway data;
- search and presentation filters;
- desktop ledger and mobile CAE cards;
- selected authorization detail;
- allocation list by location/terminal;
- server status/alert concepts without invented numbering consumption;
- shared light/dark theme tokens;
- disabled import/activate/create-allocation/more-actions controls;
- focused `verify-cae-ui.mjs` boundary guard.

## Contract limitation

The current DTO does not expose canonical consumed percentage, remaining-number count or `NextNumber`. The view does not calculate or show them.

## Mutation boundary

The hardcoded demo shell user is not authoritative permission context. Until session/permission resolution and WebApp HTTP integration are governed, all CAE mutations remain non-executable despite backend support.

## Current decision

`/cae` is implemented and eligible for merge only after repository CI and explicit human approval. Deploy and light/dark/mobile runtime acceptance remain separate gates.
