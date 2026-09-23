# UI-CAE-001 — CAE Administration Reconciliation

Status: `IMPLEMENTED_API_MOCK_DATA / VISUAL_BASELINE_APPROVED / RUNTIME_REVIEW_PENDING`

Mapping:

```text
WEB-014 -> UI-CAE-001
```

Route: `/cae`

## Product authority

`WEB-014 — CAE Administration` belongs to the Fiscal area and governs inspection and administration of CAE authorizations, ranges and allocations without allowing the client to invent numbering authority.

Accepted requirements: `FR-050..FR-056`.

Accepted roles:

- fiscal administrator;
- administrator;
- auditor.

## Accepted API dependency

WEB-014 maps exactly to:

- `API-CAE-001` `listCaeAuthorizations`;
- `API-CAE-002` `getCaeAuthorization`;
- `API-CAE-003` `importCaeAuthorization`;
- `API-CAE-004` `activateCaeAuthorization`;
- `API-CAE-005` `listCaeAllocations`;
- `API-CAE-006` `createCaeAllocation`;
- `API-CAE-007` `closeCaeAllocation`.

Wave 5 records all seven as `IMPLEMENTED` in WebApi. Reads require `fiscal.read`; mutations require `fiscal.manage_cae`.

## HTTP contract boundary

`CaeAuthorizationDto` exposes id/version, CFE type, authorization number, series, range, validity dates, textual status, verification/provenance metadata, imported/activated timestamps and optional alert code.

`CaeAllocationDto` exposes id/version, CAE authorization id, location, optional terminal, assigned range, textual status and created/closed timestamps.

The DTO does **not** expose canonical `NextNumber`, consumed percentage or remaining-number count. `UI-CAE-001` does not display or derive those values.

## Visual authority

Approved baseline: `UI-CAE-001 / v1-responsive-composite`.

- gen_id: `ea4d59d5-0a58-451f-bd34-9af9b939819b`;
- aspect ratio: `4:3`;
- authority includes desktop light plus dark/tablet/mobile compositions.

The earlier generation `a0635f18-94eb-4230-9af3-c32e37d1f37f` remains non-authoritative.

## Implemented WebApp boundary

`/cae` is now a real responsive React route registered through `UI-CAE-001` as `IMPLEMENTED_API_MOCK_DATA`.

The surface provides:

- approved master-detail hierarchy;
- local search and presentation filters;
- desktop CAE ledger;
- mobile CAE cards;
- selected authorization detail;
- allocation presentation by location/terminal;
- shared light/dark shell tokens;
- explicit loading, empty and error presentation paths;
- operation mapping for `API-CAE-001..007` in `capabilities.ts`.

Data is supplied through `gateways.cae` and deterministic fixtures while global `VITE_DATA_MODE=api` remains fail-closed.

## Why mutations remain disabled

The current shell identity `Admin / Demo Uruguay` is not authoritative session/permission context. The WebApp service layer also intentionally rejects API mode until the governed integration lane is enabled.

Therefore the React surface does not execute:

- CAE import;
- CAE activation;
- allocation creation;
- allocation closure.

The approved baseline retains those controls as disabled placement only, with explicit `fiscal.manage_cae` boundary messaging.

No direct `fetch`/Axios integration is permitted in the feature component.

## Responsive behavior

Desktop preserves the approved dense ledger plus detail/allocation panel. Tablet collapses to a single main column. At mobile widths the desktop table is replaced by stacked CAE cards, while selected detail and allocations remain readable in one column.

State is always textual and never color-only.

## Theme parity

Feature CSS uses shared shell tokens (`--surface`, `--surface-2`, `--text`, `--text-soft`, `--border`, `--accent`, etc.) rather than a feature-local fixed dark/light palette.

## Guard

`src/WebApp/scripts/verify-cae-ui.mjs` verifies:

- active WEB-014 route registration;
- `UI-CAE-001` capability mode;
- all seven API-CAE mappings;
- absence of direct HTTP integration markers;
- absence of invented numbering-consumption fields;
- shared theme-token usage;
- responsive breakpoints/mobile list.

## Runtime gate

Repository CI, explicit merge approval, deploy and deployed light/dark/mobile runtime acceptance remain separate gates. Activation of the route does not imply live API/session integration.
