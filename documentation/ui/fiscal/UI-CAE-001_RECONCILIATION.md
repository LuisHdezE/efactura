# UI-CAE-001 — CAE Administration Reconciliation

Status: `SPECIFICATION_READY / VISUAL_BASELINE_APPROVED / PLANNED_DISABLED`

Mapping:

```text
WEB-014 -> UI-CAE-001
```

Reserved route candidate: `/cae`

## Upstream product authority

`WEB-014 — CAE Administration` belongs to the Fiscal product area and exists to import, validate, monitor and operationally allocate CAE ranges/subranges without violating company-wide numbering uniqueness.

Accepted source requirements:

- `FR-050`;
- `FR-051`;
- `FR-052`;
- `FR-053`;
- `FR-054`;
- `FR-055`;
- `FR-056`.

Accepted roles:

- fiscal administrator;
- administrator;
- auditor.

Source data authority is server-owned: CAE authorization identity, range, validity, lifecycle state, verification/provenance metadata and allocation state are authoritative API data when exposed.

## Accepted WEB-014 API dependency

`documentation/blueprint-api-contract/09_INTERFACE_SCOPE_RECONCILIATION.md` maps `WEB-014` to:

- `API-CAE-001` `listCaeAuthorizations`;
- `API-CAE-002` `getCaeAuthorization`;
- `API-CAE-003` `importCaeAuthorization`;
- `API-CAE-004` `activateCaeAuthorization`;
- `API-CAE-005` `listCaeAllocations`;
- `API-CAE-006` `createCaeAllocation`;
- `API-CAE-007` `closeCaeAllocation`.

Current Wave 5 evidence records all seven operations as `IMPLEMENTED` with `CaeAuthorizationsController` WebApi evidence.

Read operations use `fiscal.read`. Import, activation and allocation mutations use `fiscal.manage_cae`.

## Current HTTP contract boundary

`CaeAuthorizationDto` currently exposes:

- id and version;
- CFE type;
- authorization number;
- series;
- range from / range to;
- valid from / valid to;
- textual status;
- verification method;
- source artifact/name/reference metadata;
- imported and activated timestamps;
- optional alert code;
- replay marker.

`CaeAllocationDto` currently exposes:

- id and CAE authorization id;
- version;
- location id;
- optional terminal id;
- range from / range to;
- textual status;
- created/closed timestamps;
- replay marker.

The HTTP DTO does **not** currently expose canonical consumed percentage, remaining-number count or `NextNumber`. The WebApp must not invent those values or derive them as authoritative CAE state.

## Visual authority

Approved baseline: `UI-CAE-001 / v1-responsive-composite`.

Approved artifact gen_id: `ea4d59d5-0a58-451f-bd34-9af9b939819b`.

Approved aspect ratio: `4:3`.

The artifact contains a primary desktop light-mode composition plus dark-mode, tablet and mobile responsive compositions within one approved image.

The pre-revision generation `a0635f18-94eb-4230-9af3-c32e37d1f37f` is not approved authority.

## Visual-to-domain reconciliation

The approved artifact governs composition, density, hierarchy, responsive behavior and visual language. It does not override source-backed CAE semantics.

The future React surface may present:

- CAE authorization list and selection;
- CFE type and series;
- authorized range;
- validity dates;
- textual lifecycle state;
- verification/provenance metadata supported by the API;
- server alert code/context;
- allocation list by location/terminal;
- allocation subranges and textual states;
- governed placement of import, activation, create-allocation and close-allocation actions.

The client must not convert illustrative labels in the approved image into new backend requirements without accepted API authority.

## Planned route boundary

`/cae` remains `PLANNED_DISABLED` until a separate implementation PR creates a real React surface and reconciles capability registration, route activation and API usage.

Baseline approval alone does not authorize:

- route activation;
- HTTP calls from the WebApp;
- import or mutation execution;
- optimistic client-side CAE state mutation;
- inferred consumed/remaining numbering state;
- changes to backend/API, Domain, Persistence, database or auth.

## Required states

The governed interface baseline requires support for:

- `default`;
- `loading`;
- `empty`;
- `error`;
- `403`;
- `409`;
- `422`.

Server error/authorization/conflict semantics must be rendered from the accepted API contract rather than locally invented status.

## Responsive behavior

The source baseline identifies CAE administration as primarily desktop/tablet, while the approved visual additionally establishes an explicit mobile adaptation.

Responsive implementation must preserve:

- textual status labels;
- authorization identity;
- range and validity context;
- allocation visibility;
- safe action hierarchy;
- theme parity.

Dense desktop tables may become stacked cards on narrow screens without losing the server-owned identity/state fields needed for auditability.

## Accessibility

- range/status information must have textual equivalents;
- state may not be encoded by color alone;
- filters and action controls must be keyboard operable;
- disabled/unauthorized operations must be explicit;
- CAE authorization, series, range and validity dates must remain readable at supported widths.

## Neighboring module boundaries

`WEB-014` does not absorb:

- `WEB-013 — Fiscal Documents`;
- `WEB-015 — Contingency and Synchronization Supervision`;
- `WEB-016 — Received CFE and XML Validation`;
- `WEB-017 — Reports and Fiscal Calendar`.

## Next gate

Preserve this approved baseline in a documentation-only PR. After explicit merge approval, implement the responsive React surface in a separate PR. HTTP integration must remain within `API-CAE-001..007` and their accepted permission/idempotency/concurrency contracts.