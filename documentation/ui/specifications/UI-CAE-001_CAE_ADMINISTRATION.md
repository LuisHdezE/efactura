# UI-CAE-001 — CAE Administration

Status: `SPECIFICATION_READY / VISUAL_BASELINE_APPROVED / PLANNED_DISABLED`

WEB mapping: `WEB-014`

Reserved route candidate: `/cae`

Approved visual baseline: `v1-responsive-composite`

Approved artifact gen_id: `ea4d59d5-0a58-451f-bd34-9af9b939819b`

Approved aspect ratio: `4:3`

## Purpose

Provide one governed CAE-administration workspace to inspect authorization ranges, validity and provenance, manage activation, inspect allocations and create/close subrange allocations without allowing the client to invent numbering authority.

## Primary roles

- fiscal administrator;
- administrator;
- auditor.

## Header and shell

The view lives inside the shared eFactura application shell under the `Fiscal` product group.

Header content:

- breadcrumb `Fiscal > CAE`;
- title `Administración de CAE`;
- concise purpose copy covering authorizations, ranges and allocations;
- primary `Importar CAE` affordance when the actor has the governed mutation permission;
- optional contextual help/guidance affordance without creating new product authority.

## Guidance surface

The approved visual contains a compact informational banner above the working surface. Its purpose is explanatory only. It must not imply regulatory advice or create a client-owned fiscal rule engine.

## Search and filters

The approved baseline reserves controls for:

- authorization number;
- CFE type;
- series/search context;
- textual lifecycle status;
- validity/date context.

Server-backed filtering must use accepted query semantics. The current list endpoint accepts `cfeType`, `page` and `pageSize`; any richer client-side filtering shown before contract expansion must be explicitly local/presentation-only and must not be represented as server query authority.

## CAE authorization ledger

Desktop uses a dense table with:

- CFE type;
- authorization number;
- series;
- authorized range;
- validity dates;
- textual status;
- allocation count when obtained from governed allocation data or an accepted projection.

The approved visual uses a chevron/selection affordance for opening the selected CAE context.

No canonical consumption percentage, remaining-number count or next-number value may be displayed unless provided by an accepted server contract.

## Selected CAE detail

The desktop detail panel and mobile stacked detail are expected to present contract-backed fields such as:

- authorization number;
- CFE type;
- series;
- range from / range to;
- validity start / end;
- textual status;
- verification method;
- source/provenance metadata where useful and safe;
- version/concurrency context where needed by governed mutations;
- alert context derived from the server-provided alert code.

The UI must not infer DGI verification beyond the server field actually returned by the contract.

## Allocations

The approved visual reserves an `Asignaciones` section associated with the selected CAE.

Each allocation may present:

- location/punto context;
- optional terminal;
- assigned range;
- textual state;
- creation/closure timestamps when useful;
- version/concurrency context as required for close operations.

The allocation list comes from `API-CAE-005`.

## Governed actions

The surface reserves placement for:

- import CAE;
- activate CAE;
- create allocation;
- close allocation.

Execution authority belongs to:

- `API-CAE-003` `importCaeAuthorization`;
- `API-CAE-004` `activateCaeAuthorization`;
- `API-CAE-006` `createCaeAllocation`;
- `API-CAE-007` `closeCaeAllocation`.

Mutation controls require `fiscal.manage_cae` and must preserve idempotency and expected-version/concurrency requirements from the accepted HTTP contract.

Read-only actors may inspect CAE and allocation data through `fiscal.read` but must not receive executable mutation controls merely because the approved visual contains their placement.

## States

Required product states:

- default;
- loading;
- empty;
- error;
- 403;
- 409;
- 422.

Additional visual state distinctions may include active/current, expired/exhausted or alert-bearing CAE when those values are returned by the server contract. Status text must remain explicit.

## Responsive behavior

Desktop:

- dense ledger and selected-detail panel side by side;
- filters above the ledger;
- allocation section inside selected detail;
- primary mutation CTA remains visually clear without overwhelming read-only information.

Tablet:

- reduced shell density;
- CAE list becomes card-oriented when table density is no longer appropriate;
- filters may collapse behind a compact filter affordance;
- detail follows selection without losing authorization/range context.

Mobile:

- one-column hierarchy;
- `Importar CAE` remains prominent only for authorized actors;
- search/filter controls compact into mobile-friendly rows;
- CAE records render as cards with authorization, type/series, range/validity summary and textual state;
- selected detail and allocation information stack vertically.

Intermediate widths may adapt while preserving information hierarchy, theme parity and execution boundaries.

## Theme parity

The approved composite explicitly includes light and dark treatments. The React implementation must use shared shell tokens rather than feature-local hard-coded theme palettes.

Light/dark acceptance requires:

- readable heading and metadata;
- coherent cards/tables/detail surfaces;
- textual status contrast;
- filters and selected-state contrast;
- no split-theme composition between shell and feature content.

## Accessibility

- CAE state is not encoded by color alone;
- range/status data keeps textual equivalents;
- table rows/cards and allocation items remain keyboard operable where interactive;
- labels are programmatically associated with filters/forms;
- authorization number, series, ranges and validity dates remain readable;
- disabled or permission-blocked actions are explicit and understandable.

## API dependencies

WEB-014 depends on:

- `API-CAE-001` list authorizations;
- `API-CAE-002` get authorization;
- `API-CAE-003` import authorization;
- `API-CAE-004` activate authorization;
- `API-CAE-005` list allocations;
- `API-CAE-006` create allocation;
- `API-CAE-007` close allocation.

Wave 5 currently records all seven operations as `IMPLEMENTED` with WebApi evidence.

## HTTP-data limitation

The current `CaeAuthorizationDto` does not expose authoritative `NextNumber`, consumed percentage or remaining-number count. The approved image is therefore reconciled to avoid treating any illustrative consumption concept as canonical product data.

Allocation count in the ledger may be produced only from loaded authoritative allocations or a future accepted projection; it must not be fabricated.

## Current implementation boundary

`WEB-014` remains `PLANNED_DISABLED`. The approved visual baseline and this specification do not activate `/cae` and do not register executable WebApp operations.

A separate implementation PR must:

1. add a real responsive React surface;
2. reconcile route/capability registration;
3. preserve shared-shell theme tokens;
4. preserve role/permission boundaries;
5. use only accepted CAE HTTP operations if integration is enabled;
6. add focused UI/contract guards;
7. pass repository CI;
8. undergo deployed runtime acceptance separately from baseline approval.