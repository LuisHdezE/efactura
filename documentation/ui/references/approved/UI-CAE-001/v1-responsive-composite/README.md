# UI-CAE-001 — Approved Visual Reference

Baseline: `v1-responsive-composite`

Status: `APPROVED_VISUAL_BASELINE`

Approval date: `2026-09-22`

Approval statement:

> aprobada

## Artifact identity

- UI: `UI-CAE-001 — CAE Administration`
- WEB mapping: `WEB-014`
- reserved route candidate: `/cae`
- approved composite image generation id: `ea4d59d5-0a58-451f-bd34-9af9b939819b`
- approved aspect ratio: `4:3`
- artifact contains a primary desktop light-mode composition plus dark-mode, tablet and mobile responsive compositions in the same approved image.

The earlier generated candidate `a0635f18-94eb-4230-9af3-c32e37d1f37f` is not the approved authority because the user requested a 4:3 revision before approval.

## Approved visual composition

The approved composite establishes presentation authority for:

- `Administración de CAE` hierarchy inside the shared eFactura shell;
- Fiscal navigation with CAE selected;
- primary `Importar CAE` affordance;
- informational guidance above the working surface;
- search and filters for authorization, CFE type, status and validity context;
- a dense desktop CAE ledger with type, authorization, series, authorized range, validity, textual status and allocation count;
- selected-CAE detail on desktop;
- allocation subtable with point/location, assigned range and textual state;
- governed action placement for activation and allocation management;
- dark-mode parity;
- tablet and mobile card-based responsive compositions.

## Functional authority boundary

The artifact is **visual authority only**. Product and API authority come from accepted WEB/API governance.

The approved baseline must not be interpreted as permission to invent data not present in the accepted CAE contract. In particular, the current HTTP DTO does not expose an authoritative consumed percentage, remaining-number count or `NextNumber`; implementations must not derive or present such values as canonical unless a later accepted contract provides them.

The baseline may visually represent:

- CAE authorization number;
- CFE type;
- series;
- authorized range;
- validity dates;
- textual status;
- verification/provenance metadata;
- alert code/context;
- allocation location/terminal, range and state.

## API authority

`WEB-014` maps to:

- `API-CAE-001` `listCaeAuthorizations`;
- `API-CAE-002` `getCaeAuthorization`;
- `API-CAE-003` `importCaeAuthorization`;
- `API-CAE-004` `activateCaeAuthorization`;
- `API-CAE-005` `listCaeAllocations`;
- `API-CAE-006` `createCaeAllocation`;
- `API-CAE-007` `closeCaeAllocation`.

Wave 5 currently records all seven operations as `IMPLEMENTED` with `CaeAuthorizationsController` WebApi evidence. This baseline still does not authorize frontend HTTP integration by itself; integration requires a separate implementation/reconciliation gate.

## Governance boundaries

This baseline does not by itself authorize:

- activation of `/cae`;
- registration of a new executable UI capability;
- live CAE claims in the WebApp before HTTP integration is reconciled;
- CAE import;
- CAE activation;
- allocation creation;
- allocation closure;
- client-side invention of numbering consumption or remaining-range authority;
- changing API/backend, domain, persistence, database or authentication behavior.

## Responsive authority

The approved 4:3 composite includes desktop light mode plus dark, tablet and mobile treatments. Intermediate widths may adapt while preserving hierarchy, information priority, textual state cues, theme parity and server-authority boundaries.

## Next gate

Before activation of `/cae`, preserve the approved baseline in repository governance, then implement `UI-CAE-001` as a responsive React surface in a separate PR. Any HTTP integration must use the accepted CAE operations and permissions without extending their semantics in the client.