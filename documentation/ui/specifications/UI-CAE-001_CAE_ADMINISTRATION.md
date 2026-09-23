# UI-CAE-001 — CAE Administration

Status: `SPECIFICATION_READY / VISUAL_BASELINE_APPROVED / IMPLEMENTED_API_MOCK_DATA / RUNTIME_REVIEW_PENDING`

WEB mapping: `WEB-014`

Route: `/cae`

Approved visual baseline: `v1-responsive-composite`

Approved artifact gen_id: `ea4d59d5-0a58-451f-bd34-9af9b939819b`

Approved aspect ratio: `4:3`

## Purpose

Provide one governed CAE-administration workspace to inspect authorization ranges, validity and provenance, inspect allocations and reserve placement for authorized CAE mutations without allowing the client to invent numbering authority.

## Primary roles

- fiscal administrator;
- administrator;
- auditor.

## Header and shell

The view lives inside the shared eFactura shell under `Fiscal`.

Header content:

- breadcrumb `Fiscal > CAE`;
- title `Administración de CAE`;
- concise purpose copy;
- contextual guide affordance;
- `Importar CAE` placement, disabled while the WebApp lacks authoritative session/permission context and governed HTTP integration.

## Data-mode boundary

The active implementation is `IMPLEMENTED_API_MOCK_DATA`.

`API-CAE-001..007` are implemented server-side and registered in `capabilities.ts`, but the current WebApp service layer still fails closed when `VITE_DATA_MODE=api`. The current shell identity is demo-only and cannot authorize `fiscal.manage_cae`.

Therefore the view uses deterministic `gateways.cae` fixtures for presentation/runtime validation. It does not issue direct HTTP calls.

## Search and filters

The active UI provides local presentation controls for:

- authorization number;
- CFE type;
- series/type text;
- textual lifecycle status;
- validity context.

The accepted server list contract currently supports `cfeType`, `page` and `pageSize`; richer mock filtering is not represented as server query authority.

## CAE authorization ledger

Desktop renders:

- CFE type;
- authorization number;
- series;
- authorized range;
- validity dates;
- textual status;
- allocation count loaded through the CAE gateway;
- selection affordance.

The current HTTP DTO does not expose canonical consumed percentage, remaining-number count or `NextNumber`. The UI must not display or infer those values.

## Selected CAE detail

The detail panel presents contract-backed concepts:

- authorization number;
- CFE type/code;
- series;
- range from/to;
- validity start/end;
- textual status;
- verification method;
- provenance/source reference;
- version;
- import timestamp;
- server alert-code context.

Fixture values are explicitly demo data and are not presented as live DGI evidence.

## Allocations

The selected CAE includes an allocation section showing:

- location;
- optional terminal;
- assigned range;
- textual allocation state.

The UI shape maps to `API-CAE-005 listCaeAllocations`. Demo allocation counts and rows are supplied by the mock gateway, not fabricated inside the component.

## Governed operations

Capability registration contains:

- `API-CAE-001` list authorizations;
- `API-CAE-002` get authorization;
- `API-CAE-003` import authorization;
- `API-CAE-004` activate authorization;
- `API-CAE-005` list allocations;
- `API-CAE-006` create allocation;
- `API-CAE-007` close allocation.

Read permission: `fiscal.read`.

Mutation permission: `fiscal.manage_cae`.

The current implementation does not execute mutations. Import, activation and allocation mutation controls remain disabled until authoritative session/permission context, idempotency handling, expected-version handling and the governed WebApp HTTP integration lane are enabled.

## States

Required product states remain:

- default;
- loading;
- empty;
- error;
- 403;
- 409;
- 422.

The mock-backed surface demonstrates default/loading/empty/error presentation. Authorization/conflict/validation states remain contract requirements for later live integration and are not faked as backend responses.

## Responsive behavior

Desktop:

- dense CAE ledger and selected-detail panel side by side;
- filters above the ledger;
- allocation section inside selected detail.

Tablet:

- workspace collapses to one main column;
- action hierarchy remains explicit.

Mobile:

- desktop table is replaced by CAE cards;
- search/action rows stack;
- selected detail and allocations become one-column surfaces;
- textual state, authorization, range and validity remain visible.

## Theme parity

Light/dark rendering uses shared shell tokens including `--surface`, `--surface-2`, `--text`, `--text-soft`, `--border`, `--accent`, `--warning-*` and `--success`.

No feature-local hard-coded theme palette is authoritative.

## Accessibility

- CAE state is not color-only;
- range/status data retains textual equivalents;
- desktop rows and mobile cards are operable controls;
- filters have labels;
- disabled server-owned actions include explanatory titles;
- authorization, series, ranges and validity dates remain readable.

## Guard and QA

`verify-cae-ui.mjs` guards route/capability registration, `API-CAE-001..007` mapping, no direct `fetch`/Axios integration, no invented numbering-consumption fields, shared theme tokens and responsive breakpoints.

The script is included in both `npm run check` and deploy build verification.

## Runtime gate

Baseline approval is not runtime acceptance. After repository gates and explicit merge approval, deployment must be reviewed separately in desktop light, desktop dark and mobile before `UI-CAE-001` is considered runtime closed.
